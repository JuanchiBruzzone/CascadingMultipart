using Microsoft.AspNetCore.Mvc;

namespace CascadingMultipart.Controllers;

[ApiController]
[Route("sample")]
public class SampleController : ControllerBase
{
    [HttpGet("Get")]
    public IActionResult GetSomething()
    {
        //ad a flog here to see if the multipart stops when failing
        if (true)
        {
            return BadRequest("No, I don't know");
        }
        
        return Ok(new { foo = "bar" });
    }

    [HttpGet("DoYouKnow")]
    public IActionResult DoYouKnow([FromQuery] string foo)
    {
        //ad a flog here to see if the multipart stops when failing
        if (true)
        {
            return BadRequest("No, I don't know");
        }
        
        return Ok(new { message = $"Yes, I know {foo}" });
    }
}