using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobExecutorModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using DevelApp.RuntimePluggableClassFactory;

namespace JobRunner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly JobDbContext _context;

        private readonly PluginClassFactory<IJobExecutor>? _jobExecutorFactory;

        public JobsController(JobDbContext context, PluginClassFactory<IJobExecutor>? jobExecutorFactory = null)
        {
            _context = context;
            _jobExecutorFactory = jobExecutorFactory;
        }

        // GET: api/Jobs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetJob()
        {
            return await _context.Job.ToListAsync();
        }

        // GET: api/Jobs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJob([FromQuery, BindRequired] int id)
        {
            var job = await _context.Job.FindAsync(id);

            if (job == null)
            {
                return NotFound();
            }

            return job;
        }

        // PUT: api/Jobs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJob([FromQuery, BindRequired] int id, [FromBody] Job job)
        {
            if (id != job.JobId)
            {
                return BadRequest();
            }
            // JobExecutor validation should be implemented via plugin factory

            _context.Entry(job).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Job>> PostJob([FromBody] PostJobRequest postJobRequest)
        {
            if (_jobExecutorFactory != null)
            {
                var executorName = new IdentifierString(postJobRequest.JobExecutor);
                var moduleName = new NamespaceString("JobExecutor");
                
                if (!_jobExecutorFactory.IsPluginAvailable(moduleName, executorName))
                {
                    return BadRequest($"Job executor {postJobRequest.JobExecutor} is not available");
                }
            }
            Job job = new Job();
            job.JobExecutor = postJobRequest.JobExecutor;
            job.JobData = postJobRequest.JobData;

            _context.Job.Add(job);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetJob", new { id = job.JobId }, job);
        }

        // DELETE: api/Jobs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob([FromQuery, BindRequired] int id)
        {
            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            _context.Job.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool JobExists(int id)
        {
            return _context.Job.Any(e => e.JobId == id);
        }
    }
}
