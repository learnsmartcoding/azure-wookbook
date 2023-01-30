using LearnSmartCoding.EssentialProducts.API.ViewModel.Create;
using LearnSmartCoding.EssentialProducts.API.ViewModel.Get;
using LearnSmartCoding.EssentialProducts.API.ViewModel.Update;
using LearnSmartCoding.EssentialProducts.Core;
using LearnSmartCoding.EssentialProducts.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LearnSmartCoding.EssentialProducts.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    //[RequiredScope("categories.read")]
    public class CategoryController : ControllerBase
    {
        IDatabase cache;
        private readonly IConfiguration configuration;
        Lazy<ConnectionMultiplexer> lazyConnection;
        public CategoryController(ILogger<CategoryController> logger,
              ICategoryService categoryService, IConfiguration configuration)
        {
            Logger = logger;
            CategoryService = categoryService;
            this.configuration = configuration;

            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                var cacheConnectionString = configuration.GetConnectionString("CacheConnection");

                return ConnectionMultiplexer.Connect(cacheConnectionString);
            });

            cache = lazyConnection.Value.GetDatabase();
        }

        public ILogger<CategoryController> Logger { get; }
        public ICategoryService CategoryService { get; }


        [HttpGet("", Name = "GetCacheDetails")]      
        public async Task<IActionResult> GetCacheDetails()
        {
            var model = new CacheModel();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Running the PING command");
            builder.AppendLine($"Response: {cache.Execute("PING").ToString()}");
            builder.AppendLine();

            builder.AppendLine("Running the FLUSHALL command");
            builder.AppendLine($"Response: {cache.Execute("FLUSHALL").ToString()}");
            builder.AppendLine();

            model.CacheLog = builder.ToString();

            // Get the client list

            model.CacheClients = cache.Execute("CLIENT", "LIST").ToString().Replace(
                                          "id=", "\rid="); ;
            lazyConnection.Value.Dispose();

            return Ok(model);

        }


        [HttpGet("{id}", Name = "GetCategory")]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound)]       
        public async Task<IActionResult> GetCategoryAsync([FromRoute] short id)
        {            
            Logger.LogInformation($"Executing {nameof(GetCategoryAsync)}");
            
            var category = new Category();

            var keyName = $"category-{id}";
            //Check if it is in cache
            var cachedData = cache.StringGet(keyName);

            if (cachedData.HasValue)
                category = JsonConvert.DeserializeObject<Category>(cachedData.ToString());
            else
                category = await CategoryService.GetCategoryAsync(id);

            if (category == null)
                return NotFound();

            //if not present in cache, save it in cache
            if (!cachedData.HasValue)
            {
                cache.StringSet(keyName, JsonConvert.SerializeObject(category));
            }

            return Ok(category);
        }

        [HttpGet("All", Name = "GetAllCategory")]
        [ProducesResponseType(typeof(List<Category>), StatusCodes.Status200OK)]       
        public async Task<IActionResult> GetAllCategoryAsync()
        {
            Logger.LogInformation($"Executing {nameof(GetCategoryAsync)}");

            var categories = new List<Category>();

            var keyName = "categories";

            //Check if it is in cache
            var cachedData = cache.StringGet(keyName);

            if (cachedData.HasValue)
                categories = JsonConvert.DeserializeObject<List<Category>>(cachedData.ToString());
            else
                categories = await CategoryService.GetCategoriesAsync();

            //if not present in cache, save it in cache
            if (!cachedData.HasValue)
            {
                cache.StringSet(keyName, JsonConvert.SerializeObject(categories));
            }           

            return Ok(categories);

        }


        [HttpPost("", Name = "PostCategory")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]        
        public async Task<IActionResult> PostCategoryAsync([FromBody] CreateCategory createCategory)
        {            
            Logger.LogInformation($"Executing {nameof(PostCategoryAsync)}");

            var entity = new Category() { IsActive = createCategory.IsActive, Name = createCategory.Name };

            var isSuccess = await CategoryService.CreateCategoryAsync(entity);

            return new CreatedAtRouteResult("GetCategory",
                   new { id = entity.Id });
        }


        [HttpPut("", Name = "PutCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]       
        public async Task<IActionResult> PutCategoryAsync([FromBody] UpdateCategory updateCategory)
        {
            Logger.LogInformation($"Executing {nameof(PutCategoryAsync)}");
            
            var keyName = $"category-{updateCategory.Id}";

            //Check if it is in cache, remove it
            var cachedData = cache.StringGet(keyName);
            if (cachedData.HasValue)
            {
                await cache.KeyDeleteAsync(keyName);
            }
            var entity = new Category() { Id = updateCategory.Id, IsActive = updateCategory.IsActive, Name = updateCategory.Name };

            await CategoryService.UpdateCategoryAsync(entity);

            return Ok();
        }


        [HttpDelete("{id}", Name = "DeleteCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateDictionary), StatusCodes.Status400BadRequest)]        
        public async Task<IActionResult> DeleteCategoryAsync([FromRoute] short id)
        {
            Logger.LogInformation($"Executing {nameof(DeleteCategoryAsync)}");

            var category = await CategoryService.GetCategoryAsync(id);

            if (category == null)
                return NotFound();

            var keyName = $"category-{id}";

            //Check if it is in cache, remove it
            var cachedData = cache.StringGet(keyName);
            if (cachedData.HasValue)
            {
                await cache.KeyDeleteAsync(keyName);
            }

            await CategoryService.DeleteCategoryAsync(id);

            return Ok();
        }

    }

    public class CacheModel
    {
        public string CacheLog { get; internal set; }
        public string CacheClients { get; internal set; }
    }
}

