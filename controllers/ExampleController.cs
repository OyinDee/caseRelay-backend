using Microsoft.AspNetCore.Mvc;

namespace CaseRelayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExampleController : ControllerBase
    {
        /// <summary>
        /// Gets an example item.
        /// </summary>
        /// <returns>An example item.</returns>
        [HttpGet]
        public IActionResult GetExample()
        {
            // ...existing code...
            return Ok(new { Message = "This is an example." });
        }

        /// <summary>
        /// Creates a new example item.
        /// </summary>
        /// <param name="example">The example item to create.</param>
        /// <returns>The created example item.</returns>
        [HttpPost]
        public IActionResult CreateExample([FromBody] object example)
        {
            // ...existing code...
            return CreatedAtAction(nameof(GetExample), new { id = 1 }, example);
        }
    }
}
