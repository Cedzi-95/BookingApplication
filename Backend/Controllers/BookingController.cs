using System.Security.Claims;
using BookingApplication.Exceptions;
using BookingApplication.Interfaces;
using BookingApplication.Models;
using BookingApplication.Models.Dtos;
using BookingApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BookingApplication.Controllers;

[ApiController]
[Route("booking")]
public class BookingController(IService<Booking, CreateBookingRequest, EditBookingRequest> bookingService) : ControllerBase
{

    [HttpGet("{bookingId}")]
    public async Task<IActionResult> GetBooking(Guid bookingId)
    {
        try
        {
            var booking = await bookingService.GetByIdAsync(bookingId);

            if (booking is null)
            {
                return NotFound("No booking was found with that id");
            }

            return Ok(BookingResponse.FromModel(booking));

        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Book([FromBody] CreateBookingRequest request)
    {
        try
        {
            if (request.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Forbid("User id does not match logged in user. To book for someone else, please login as an admin");
            }

            var booking = await bookingService.CreateFromRequestAsync(request);
            return Created(nameof(Book), new { Message = $"Created booking with Id {booking.Id}" });
        }
        catch (DateErrorException e)
        {
            return BadRequest($"An error with request dates have occured: {e.Message}");
        }
        catch (ArgumentException e)
        {
            return BadRequest($"The request could not be processed: {e.Message}");
        }
        catch (Exception)
        {
            return BadRequest("An unspecified error has happened");
        }
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] EditBookingRequest request)
    {
        try
        {
            if (request.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Unauthorized("User id does not match logged in user. To edit a booking for someone else or transfer a booking to someone else, please login as an admin");
            }

            var booking = await bookingService.EditFromRequestAsync(request);
            return Created(nameof(Edit), new { Message = $"Edited booking with Id {booking.Id}", Booking = booking });
        }
        catch (ArgumentException e)
        {
            return BadRequest($"The request could not be processed: {e.Message}");
        }
        catch (Exception)
        {
            return BadRequest("An unspecified error has happened");
        }
    }

    [Authorize]
    [HttpDelete("{bookingId}")] //
    public async Task<IActionResult> Cancel(Guid bookingId)
    {
        try
        {
            var booking = await bookingService.GetByIdAsync(bookingId);
            if (booking?.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
            {
                return Forbid("To cancel someone else's booking, please log in as an admin.");
            }
            await bookingService.DeleteAsync(bookingId);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest("An unspecified error has happened");
        }
    }

    [HttpGet("/room/{roomId}")]
    public async Task<IActionResult> GetAllBookingsForRoom(Guid roomId)
    {
        try
        {
            // sligtly more business logic then we want in a controller
            // but changing the Iservice and IRepository just for this function
            // this late in a project seems a bit over the top. So I did it this way.
            var bookings = await bookingService.GetAllAsync();
            return Ok(bookings.Where(b => b.RoomId == roomId));
        }
        catch (Exception)
        {
            return BadRequest("An unspecified error has happened");
        }
    }
}


