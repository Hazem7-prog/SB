using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SB.DTOs;
using SB.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ChildController : ControllerBase
    {
        private readonly IChildService _childService;
        private readonly ILogger<ChildController> _logger;

        public ChildController(IChildService childService, ILogger<ChildController> logger)
        {
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("CreateChild")]
        [AllowAnonymous]
        public async Task<ActionResult<ChildResponse>> Create([FromBody] ChildRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var created = await _childService.AddChildAsync(request);
                return CreatedAtAction(nameof(GetChildById), new { id = created.ChildId }, created);
            }
            catch (ValidationException vex)
            {
                ModelState.AddModelError(nameof(request.SimCardNum), vex.Message);
                return ValidationProblem(ModelState);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DB error creating child - possible uniqueness or FK violation");
                return Conflict(new { message = "Database error creating child." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating child");
                return Problem(detail: "An unexpected error occurred.", statusCode: 500);
            }
        }

        [HttpGet("GetAllChildren")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ChildResponse>>> GetAll()
        {
            var list = await _childService.GetAllChildrenAsync();
            return Ok(list);
        }

        [HttpGet("GetChildById/{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ChildResponse>> GetChildById(int id)
        {
            var child = await _childService.GetChildByIdAsync(id);
            return Ok(child);
        }

        [HttpPut("UpdateChild/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Update(int id, [FromBody] ChildUpdate request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                await _childService.UpdateChildAsync(id, request);
                return NoContent();
            }
            catch (ValidationException vex)
            {
                ModelState.AddModelError(nameof(request.SimCardNum), vex.Message);
                return ValidationProblem(ModelState);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DB error updating child - possible uniqueness constraint violation");
                return Conflict(new { message = "This phone number is already used." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating child");
                return Problem(detail: "An unexpected error occurred.", statusCode: 500);
            }
        }

        [HttpDelete("DeleteChild/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _childService.DeleteChildAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting child");
                return Problem(detail: "An unexpected error occurred.", statusCode: 500);
            }
        }
    }
}