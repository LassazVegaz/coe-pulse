using COEPulse.API.DTO;
using COEPulse.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COEPulse.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class IndexController(DataService ds)
        : ControllerBase
    {
        [HttpGet]
        public IEnumerable<COERecord> Get([FromQuery] Filters filters)
        {
            return ds.GetRecords(filters);
        }
    }
}
