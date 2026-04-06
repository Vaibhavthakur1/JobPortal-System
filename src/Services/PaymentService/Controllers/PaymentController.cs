using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services;
using System.Security.Claims;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize(Roles = "Recruiter")]
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] PaymentInitRequest request)
    {
        var result = await paymentService.InitiatePaymentAsync(CurrentUserId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] PurchasePointsRequest request)
    {
        var result = await paymentService.ConfirmPaymentAsync(CurrentUserId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var result = await paymentService.GetWalletAsync(CurrentUserId);
        return Ok(result);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await paymentService.GetTransactionHistoryAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    // Internal endpoint called by RecruiterService to deduct points
    [Authorize(Roles = "Recruiter,Admin")]
    [HttpPost("deduct")]
    public async Task<IActionResult> Deduct([FromBody] DeductPointsRequest request)
    {
        var result = await paymentService.DeductPointsAsync(request);
        return Ok(result);
    }
}
