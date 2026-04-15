using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SB.Controllers
{
    [ApiController]
    [Route("ApiController")]
    public class ApiController : ControllerBase
    {
        private readonly EndpointDataSource _endpointDataSource;

        public ApiController(EndpointDataSource endpointDataSource)
        {
            _endpointDataSource = endpointDataSource;
        }

        [HttpGet]
        public IActionResult GetRoutes()
        {
            var endpoints = _endpointDataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Select(e => e.RoutePattern.RawText);

            return Ok(endpoints);
        }
    }
}
