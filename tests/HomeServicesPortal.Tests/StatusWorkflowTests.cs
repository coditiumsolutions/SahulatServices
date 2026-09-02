using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Xunit;

namespace HomeServicesPortal.Tests;

/// <summary>
/// Integration tests for docs/status-workflow.md against a real database (whatever
/// appsettings.json currently points at). Every row a test inserts is deleted at the end of that
/// same test, in a finally block, so failures still clean up. The shared Client/Provider/Address
/// fixture is cleaned up in DatabaseFixture.DisposeAsync.
/// </summary>
[Collection("StatusWorkflow")]
public class StatusWorkflowTests
{
    private readonly DatabaseFixture _fixture;

    public StatusWorkflowTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // ---- helpers -------------------------------------------------------

    private async Task<int> InsertRequestAsync(
        DateOnly? preferredServiceDate = null,
        string? preferredServiceTime = null)
    {
        await using var db = _fixture.CreateContext();
        var request = new CustomerServiceRequest
        {
            ClientUid = _fixture.ClientUid,
            CategoryUid = _fixture.CategoryUid,
            ClientAddressUid = _fixture.ClientAddressUid,
            ServiceTitle = "AUTOTEST Service",
            ContactNo = "9000000000",
            Status = "Pending",
            PreferredServiceDate = preferredServiceDate,
            PreferredServiceTime = preferredServiceTime,
            CreatedOn = DateTime.Now
        };
        db.CustomerServiceRequests.Add(request);
        await db.SaveChangesAsync();
        return request.Uid;
    }

    private async Task<int> InsertBookingAsync(int requestUid, string status)
    {
        await using var db = _fixture.CreateContext();
        var booking = new ServiceBooking
        {
            RequestUid = requestUid,
            ClientUid = _fixture.ClientUid,
            ProviderUid = _fixture.ProviderUid,
            EstimatedAmount = 1000,
            FinalAmount = 1000,
            PaymentMode = "CashToProvider",
            CommissionType = "Percent",
            CommissionValue = 10,
            Status = status,
            Passcode = status is "Accepted" or "In Progress" or "Completed" or "Closed" ? "1234" : null,
            CreatedOn = DateTime.Now
        };
        db.ServiceBookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Uid;
    }

    private async Task DeleteRequestAndBookingAsync(int requestUid, int? bookingUid)
    {
        await using var db = _fixture.CreateContext();
        if (bookingUid.HasValue)
        {
            var booking = await db.ServiceBookings.FindAsync(bookingUid.Value);
            if (booking != null) db.ServiceBookings.Remove(booking);
        }

        var request = await db.CustomerServiceRequests.FindAsync(requestUid);
        if (request != null) db.CustomerServiceRequests.Remove(request);

        await db.SaveChangesAsync();
    }

    // ---- client whitelist (Implementation note #2) ---------------------

    [Fact]
    public async Task UpdateRequestAsync_RejectsClientSelfReportingCompleted()
    {
        var requestUid = await InsertRequestAsync();
        try
        {
            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);

            var (success, error, data) = await service.UpdateRequestAsync(new UpdateCustomerServiceRequestDto
            {
                RequestUid = requestUid,
                CategoryUid = _fixture.CategoryUid,
                ClientAddressUid = _fixture.ClientAddressUid,
                ServiceTitle = "AUTOTEST Service",
                ContactNo = "9000000000",
                Status = "Completed" // disallowed for a client PUT
            });

            Assert.False(success);
            Assert.Null(data);
            Assert.Equal("Invalid status value.", error);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, null);
        }
    }

    [Fact]
    public async Task UpdateRequestAsync_AllowsClientCancelWithReason()
    {
        var requestUid = await InsertRequestAsync();
        try
        {
            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);

            var (success, error, data) = await service.UpdateRequestAsync(new UpdateCustomerServiceRequestDto
            {
                RequestUid = requestUid,
                CategoryUid = _fixture.CategoryUid,
                ClientAddressUid = _fixture.ClientAddressUid,
                ServiceTitle = "AUTOTEST Service",
                ContactNo = "9000000000",
                Status = "Cancelled",
                CancelReason = "AUTOTEST cancel reason"
            });

            Assert.True(success, error);
            Assert.NotNull(data);
            Assert.Equal("Cancelled", data!.Status);
            Assert.Equal("AUTOTEST cancel reason", data.CancelReason);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, null);
        }
    }

    // ---- progressStatus (Implementation note #5) ------------------------

    [Fact]
    public async Task ProgressStatus_NoBooking_IsRequested()
    {
        var requestUid = await InsertRequestAsync();
        try
        {
            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);

            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            Assert.Equal("Requested", data!.ProgressStatus);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, null);
        }
    }

    [Fact]
    public async Task ProgressStatus_BookingPendingOrAccepted_BeforeSchedule_IsAssigned()
    {
        var requestUid = await InsertRequestAsync(
            preferredServiceDate: DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            preferredServiceTime: "09:00");
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Accepted");

            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);
            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            Assert.Equal("Assigned", data!.ProgressStatus);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    [Fact]
    public async Task ProgressStatus_BookingAccepted_ScheduleTimeArrived_IsInProgress()
    {
        var requestUid = await InsertRequestAsync(
            preferredServiceDate: DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
            preferredServiceTime: "09:00");
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Accepted");

            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);
            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            Assert.Equal("In Progress", data!.ProgressStatus);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    [Fact]
    public async Task ProgressStatus_BookingInProgress_IsInProgress()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "In Progress");

            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);
            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            Assert.Equal("In Progress", data!.ProgressStatus);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Closed")]
    public async Task ProgressStatus_BookingCompletedOrClosed_IsCompleted(string bookingStatus)
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, bookingStatus);

            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);
            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            Assert.Equal("Completed", data!.ProgressStatus);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    [Fact]
    public async Task ProgressStatus_BookingCancelled_IsNullNotACancelledStage()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Cancelled");

            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);
            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            // Per the "Cancelled excluded from progressStatus entirely" rule — the progress bar
            // hides, it never shows a "Cancelled" stage.
            Assert.Null(data!.ProgressStatus);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    // ---- Closed visibility gate (Implementation note #3) -----------------

    [Fact]
    public async Task ProviderContactFields_VisibleWhenBookingClosed()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Closed");

            await using var db = _fixture.CreateContext();
            var service = new CustomerServiceRequestService(db);
            var (success, _, data) = await service.GetRequestByIdAsync(requestUid);

            Assert.True(success);
            Assert.Equal(_fixture.ProviderUid, data!.ProviderUid);
            Assert.NotNull(data.ProviderMobileNo);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    // ---- POST /service-bookings/{id}/start (Implementation note #4) ------

    [Fact]
    public async Task StartJobAsync_TransitionsAcceptedToInProgress()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Accepted");

            await using var db = _fixture.CreateContext();
            var bookingService = new BookingService(db, new PaymentService(db));

            var (success, error) = await bookingService.StartJobAsync(bookingUid.Value, _fixture.ProviderUid);
            Assert.True(success, error);

            await using var verifyDb = _fixture.CreateContext();
            var booking = await verifyDb.ServiceBookings.FindAsync(bookingUid.Value);
            Assert.Equal("In Progress", booking!.Status);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    [Fact]
    public async Task StartJobAsync_RejectsWhenBookingNotAccepted()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Pending");

            await using var db = _fixture.CreateContext();
            var bookingService = new BookingService(db, new PaymentService(db));

            var (success, error) = await bookingService.StartJobAsync(bookingUid.Value, _fixture.ProviderUid);

            Assert.False(success);
            Assert.Equal("This booking is not awaiting start.", error);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    [Fact]
    public async Task StartJobAsync_RejectsWrongProvider()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Accepted");

            await using var db = _fixture.CreateContext();
            var bookingService = new BookingService(db, new PaymentService(db));

            var (success, error) = await bookingService.StartJobAsync(bookingUid.Value, _fixture.ProviderUid + 999_999);

            Assert.False(success);
            Assert.Equal("Booking not found.", error);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }

    // ---- post-acceptance cancel sync (Implementation note #6) -------------

    [Fact]
    public async Task PostAcceptanceCancel_SyncsRequestStatusToCancelled()
    {
        var requestUid = await InsertRequestAsync();
        int? bookingUid = null;
        try
        {
            bookingUid = await InsertBookingAsync(requestUid, "Accepted");

            // The request row mirrors what AssignProviderAsync would have set it to.
            await using (var seedDb = _fixture.CreateContext())
            {
                var request = await seedDb.CustomerServiceRequests.FindAsync(requestUid);
                request!.Status = "Assigned";
                await seedDb.SaveChangesAsync();
            }

            await using var db = _fixture.CreateContext();
            var bookingService = new BookingService(db, new PaymentService(db));

            var form = new BookingFormVm
            {
                Uid = bookingUid.Value,
                RequestUid = requestUid,
                ProviderUid = _fixture.ProviderUid,
                EstimatedAmount = 1000,
                VisitCharges = 0,
                AdditionalCharges = 0,
                Deductions = 0,
                FinalAmount = 1000,
                CustomerPaid = 0,
                PaymentMode = "CashToProvider",
                CommissionType = "Percent",
                CommissionValue = 10,
                Status = "Cancelled",
                CancelReason = "AUTOTEST provider unavailable"
            };

            var (success, error) = await bookingService.UpdateAsync(form);
            Assert.True(success, error);

            await using var verifyDb = _fixture.CreateContext();
            var booking = await verifyDb.ServiceBookings.FindAsync(bookingUid.Value);
            var request2 = await verifyDb.CustomerServiceRequests.FindAsync(requestUid);

            Assert.Equal("Cancelled", booking!.Status);
            Assert.Equal("AUTOTEST provider unavailable", booking.CancelReason);
            Assert.Equal("Cancelled", request2!.Status);
            Assert.Equal("AUTOTEST provider unavailable", request2.CancelReason);
        }
        finally
        {
            await DeleteRequestAndBookingAsync(requestUid, bookingUid);
        }
    }
}
