using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using p48_lessons_asp.Data;
using p48_lessons_asp.Data.Entity;
using p48_lessons_asp.Models;
using p48_lessons_asp.Services.StorageService;

namespace p48_lessons_asp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductImagesController : ControllerBase
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<ProductImagesController> _logger;
        private readonly IStorageService _storageService;

        public ProductImagesController(
            DataContext dataContext,
            ILogger<ProductImagesController> logger,
            IStorageService storageService)
        {
            _dataContext = dataContext;
            _logger = logger;
            _storageService = storageService;
        }

        public class UploadProductImageRequest
        {
            public Guid ProductId { get; set; }
            public IFormFile Image { get; set; } = null!;
        }

        [HttpPost]
        public async Task<IActionResult> AddProductImage([FromForm] UploadProductImageRequest model)
        {
            try
            {
                _logger.LogInformation("Спроба додавання зображення до продукту з ID: {ProductId}", model.ProductId);

                if (model.Image == null || model.Image.Length == 0)
                {
                    _logger.LogWarning("Передано порожній або відсутній файл зображення для продукту ID: {ProductId}", model.ProductId);
                    return BadRequest(new ResponseModel<object>
                    {
                        StatusCode = 400,
                        Message = "Image file is required"
                    });
                }

                var product = await _dataContext.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Продукт з ID {ProductId} не знайдено в БД при додаванні зображення.", model.ProductId);
                    return NotFound(new ResponseModel<object>
                    {
                        StatusCode = 404,
                        Message = "Product not found"
                    });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(fileStream);
                }

                var productImage = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    Url = $"/images/{uniqueFileName}",
                    ProductId = product.Id
                };

                _dataContext.ProductImages.Add(productImage);
                await _dataContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Об'єкт ProductImage успішно створено в БД! ImageID: {ImageId}, ProductID: {ProductId}, Url: {ImageUrl}",
                    productImage.Id,
                    productImage.ProductId,
                    productImage.Url
                );

                return StatusCode(201, new ResponseModel<ProductImage>
                {
                    StatusCode = 201,
                    Message = "Product image successfully created",
                    Data = productImage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Сталася помилка під час додавання зображення до продукту {ProductId}: {ErrorMessage}", model.ProductId, ex.Message);
                return StatusCode(500, new ResponseModel<object>
                {
                    StatusCode = 500,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductImages()
        {
            try
            {
                _logger.LogInformation("Запит списку всіх зображень продуктів.");

                var images = await _dataContext.ProductImages.ToListAsync();

                if (images == null || images.Count == 0)
                {
                    _logger.LogWarning("Зображення продуктів у БД не знайдено.");
                    return StatusCode(404, new ResponseModel<object>
                    {
                        StatusCode = 404,
                        Message = "Product images not found"
                    });
                }

                return StatusCode(200, new ResponseModel<List<ProductImage>>
                {
                    StatusCode = 200,
                    Message = "Ok",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні списку зображень продуктів: {ErrorMessage}", ex.Message);
                return StatusCode(500, new ResponseModel<object>
                {
                    StatusCode = 500,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductImage(Guid id)
        {
            try
            {
                _logger.LogInformation("Спроба видалення зображення з ID: {ImageId}", id);

                var productImage = await _dataContext.ProductImages.FindAsync(id);
                if (productImage == null)
                {
                    _logger.LogWarning("Зображення з ID {ImageId} не знайдено в БД.", id);
                    return StatusCode(404, new ResponseModel<object>
                    {
                        StatusCode = 404,
                        Message = "Product image not found"
                    });
                }

                await _storageService.DeleteFileAsync(productImage.Url);

                _dataContext.ProductImages.Remove(productImage);
                await _dataContext.SaveChangesAsync();

                _logger.LogInformation("Зображення з ID {ImageId} успішно видалено з БД", id);

                return StatusCode(200, new ResponseModel<object>
                {
                    StatusCode = 200,
                    Message = "Product image successfully deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні зображення з ID {ImageId}: {ErrorMessage}", id, ex.Message);
                return StatusCode(500, new ResponseModel<object>
                {
                    StatusCode = 500,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }
    }
}