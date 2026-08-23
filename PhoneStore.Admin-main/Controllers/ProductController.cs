using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using PhoneStore.Models;
using PhoneStore.Services;
using PhoneStore.Attributes;

namespace PhoneStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly PhoneStoreContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IProductImageService _imageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            PhoneStoreContext context,
            IWebHostEnvironment webHostEnvironment,
            IProductImageService imageService,            ILogger<ProductController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }        [AdminAuthorize(area: "Product", action: "Index")]
        public async Task<IActionResult> Index(string searchTerm, string category, string color, string sortBy, string publishStatus, int page = 1)
        {
            _logger.LogInformation("Product Index called with parameters: searchTerm={SearchTerm}, category={Category}, color={Color}, sortBy={SortBy}, publishStatus={PublishStatus}, page={Page}", 
                searchTerm, category, color, sortBy, publishStatus, page);

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Color)
                .Include(p => p.Discount)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    (p.ProductName != null && p.ProductName.Contains(searchTerm)) ||
                    (p.DetailDescription != null && p.DetailDescription.Contains(searchTerm)));
            }

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int categoryId))
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }            // Lọc theo màu sắc
            if (!string.IsNullOrEmpty(color) && int.TryParse(color, out int colorId))
            {
                query = query.Where(p => p.ProductImages.Any(pi => pi.ColorId == colorId));
            }

            // Lọc theo trạng thái publish
            if (!string.IsNullOrEmpty(publishStatus) && bool.TryParse(publishStatus, out bool isPublished))
            {
                query = query.Where(p => p.IsPublished == isPublished);
            }

            // Sắp xếp
            query = sortBy switch
            {
                "name" => query.OrderBy(p => p.ProductName),
                "price" => query.OrderBy(p => p.Price),
                "stock" => query.OrderBy(p => p.Stock),
                _ => query.OrderByDescending(p => p.ProductId)
            };            // Phân trang
            int pageSize = 12;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedColor = color;
            ViewBag.SortBy = sortBy;
            ViewBag.PublishStatus = publishStatus;

            // Lấy danh sách danh mục cho dropdown
            ViewBag.Categories = await _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName ?? string.Empty
                })
                .ToListAsync();

            // Lấy danh sách màu sắc cho dropdown
            ViewBag.Colors = await _context.Colors
                .Select(c => new SelectListItem
                {
                    Value = c.ColorId.ToString(),
                    Text = c.ColorName ?? string.Empty
                })
                .ToListAsync();

            return View(products);
        }        [AdminAuthorize(area: "Product", action: "Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
            ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
            ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName");
            return View();
        }        [AdminAuthorize(area: "Product", action: "Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)] // 100MB total limit
        public async Task<IActionResult> Create(Product product)
        {
            _logger.LogInformation("=== CREATE PRODUCT REQUEST STARTED ===");
            _logger.LogInformation("Product data received: {@Product}", product);

            // Extract colors from form
            var colors = new List<int>();
            var imagesByColor = new Dictionary<int, List<IFormFile>>();

            // Log form data
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form key: {Key}, Value count: {Count}", key, Request.Form[key].Count);
            }

            // Get all color values first
            if (Request.Form.ContainsKey("Colors"))
            {
                var colorValues = Request.Form["Colors"];
                foreach (var colorValue in colorValues)
                {
                    _logger.LogInformation("Processing color value: {ColorValue}", colorValue);
                    if (int.TryParse(colorValue, out int colorId) && colorId > 0)
                    {
                        colors.Add(colorId);
                        // Initialize the image list for this color
                        imagesByColor[colorId] = new List<IFormFile>();
                    }
                }
            }

            _logger.LogInformation("Found {Count} colors: {Colors}",
                colors.Count,
                string.Join(", ", colors));

            // Log all files to help with debugging
            _logger.LogInformation("Total files in request: {Count}", Request.Form.Files.Count);
            foreach (var file in Request.Form.Files)
            {
                _logger.LogInformation("File: {Name}, FileName: {FileName}, Size: {Size}",
                    file.Name, file.FileName, file.Length);
            }

            // Group files by their form name index
            var filesByIndex = new Dictionary<int, List<IFormFile>>();

            for (int i = 0; i < Request.Form.Files.Count; i++)
            {
                var file = Request.Form.Files[i];
                // The name should follow format "Images" for multiple files for the same color
                if (file.Name == "Images")
                {
                    int currentIndex = Math.Min(i, colors.Count - 1);
                    if (currentIndex >= 0 && currentIndex < colors.Count)
                    {
                        int colorId = colors[currentIndex];
                        imagesByColor[colorId].Add(file);
                        _logger.LogInformation("Added file {FileName} to color {ColorId} at index {Index}",
                            file.FileName, colorId, currentIndex);
                    }
                }
            }

            _logger.LogInformation("Processed {Count} color groups with images", imagesByColor.Count);
              async Task<IActionResult> PrepareViewBagsAndReturn()
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
                ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
                ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName", product.DiscountId);
                return View(product);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                return await PrepareViewBagsAndReturn();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (colors == null || colors.Count == 0)
                {
                    _logger.LogWarning("No colors selected");
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất một màu sắc");
                    return await PrepareViewBagsAndReturn();
                }

                // Validate that each color has at least one image
                foreach (var colorId in colors)
                {
                    if (!imagesByColor.ContainsKey(colorId) || imagesByColor[colorId].Count == 0)
                    {
                        _logger.LogWarning("No images provided for color {ColorId}", colorId);
                        ModelState.AddModelError("", $"Vui lòng chọn ít nhất một ảnh cho mỗi màu sắc");
                        return await PrepareViewBagsAndReturn();
                    }
                }

                // First save the product
                _context.Products.Add(product);
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("Created product with ID: {ProductId}, SaveChanges result: {SaveResult}",
                    product.ProductId, saveResult);

                if (saveResult <= 0)
                {
                    _logger.LogError("Failed to save product to database");
                    ModelState.AddModelError("", "Không thể lưu thông tin sản phẩm");
                    return await PrepareViewBagsAndReturn();
                }

                // Process images for each color
                foreach (var colorId in colors)
                {
                    var colorImages = imagesByColor[colorId];
                    _logger.LogInformation("Processing {Count} images for color {ColorId}",
                        colorImages.Count, colorId);

                    foreach (var image in colorImages.Where(i => i != null && i.Length > 0))
                    {
                        try
                        {
                            _logger.LogInformation("Processing image {FileName} ({Length} bytes) for color {ColorId}",
                                image.FileName, image.Length, colorId);

                            var imageData = await _imageService.ProcessImageAsync(image);
                            if (imageData == null)
                            {
                                _logger.LogWarning("Image processing returned null for {FileName}", image.FileName);
                                continue;
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = product.ProductId,
                                ColorId = colorId,
                                ImageData = imageData,
                                ImageMimeType = "image/jpeg"
                            };

                            _context.ProductImages.Add(productImage);
                            var imageSaveResult = await _context.SaveChangesAsync();

                            _logger.LogInformation(
                                "Saved image for product {ProductId}, color {ColorId}, size {Size} bytes, SaveChanges result: {SaveResult}",
                                product.ProductId,
                                colorId,
                                imageData.Length,
                                imageSaveResult);

                            if (imageSaveResult <= 0)
                            {
                                _logger.LogError("Failed to save image to database");
                                ModelState.AddModelError("", "Không thể lưu hình ảnh sản phẩm");
                                await transaction.RollbackAsync();
                                return await PrepareViewBagsAndReturn();
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            _logger.LogError(ex, "Validation error processing image for product {ProductId}", product.ProductId);
                            ModelState.AddModelError("", ex.Message);
                            await transaction.RollbackAsync();
                            return await PrepareViewBagsAndReturn();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing image for product {ProductId}", product.ProductId);
                            ModelState.AddModelError("", $"Lỗi xử lý ảnh: {ex.Message}");
                            await transaction.RollbackAsync();
                            return await PrepareViewBagsAndReturn();
                        }
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully completed product creation for {ProductId}", product.ProductId);
                TempData["Success"] = "Thêm sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product: {Message}", ex.Message);
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
                return await PrepareViewBagsAndReturn();
            }        }        [AdminAuthorize(area: "Product", action: "Delete")]
        [HttpPost]
        [Route("Product/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction(nameof(Index));
                }

                // Check if product is referenced in any orders
                var hasOrderReferences = await _context.OrderDetails
                    .AnyAsync(od => od.ProductId == id);

                if (hasOrderReferences)
                {
                    _logger.LogWarning("Cannot delete product with ID {ProductId} - referenced in orders", id);
                    TempData["Error"] = "Không thể xóa sản phẩm này vì đã có đơn hàng sử dụng sản phẩm này";
                    return RedirectToAction(nameof(Index));
                }

                // Delete product images first
                if (product.ProductImages != null && product.ProductImages.Any())
                {
                    _logger.LogInformation("Deleting {Count} images for product {ProductId}",
                        product.ProductImages.Count, id);
                    _context.ProductImages.RemoveRange(product.ProductImages);
                    await _context.SaveChangesAsync();
                }

                // Then delete the product
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Product with ID {ProductId} successfully deleted", id);
                TempData["Success"] = "Xóa sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting product with ID {ProductId}: {Message}", id, ex.Message);

                // Provide more specific error messages
                string errorMessage = "Lỗi khi xóa sản phẩm";
                if (ex.Message.Contains("REFERENCE constraint") || ex.Message.Contains("foreign key"))
                {
                    errorMessage = "Không thể xóa sản phẩm này vì đang được sử dụng trong hệ thống";
                }
                else if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
                    errorMessage += ": " + ex.InnerException.Message;
                }
                else
                {
                    errorMessage += ": " + ex.Message;
                }

                TempData["Error"] = errorMessage;
                return RedirectToAction(nameof(Index));
            }
        }[HttpGet]
        [Route("Product/GetColorImages")]
        [Route("Product/GetColorImages/{productId:int}/{colorId:int}")]
        public async Task<IActionResult> GetColorImages(int productId, int colorId)
        {
            try
            {
                _logger.LogInformation("Getting images for product {ProductId}, color {ColorId}", productId, colorId);

                if (productId <= 0 || colorId <= 0)
                {
                    _logger.LogWarning("Invalid parameters: productId={ProductId}, colorId={ColorId}", productId, colorId);
                    return Json(new { success = false, message = "ID sản phẩm hoặc màu không hợp lệ" });
                }

                var images = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId && pi.ColorId == colorId)
                    .Select(pi => new {
                        imageId = pi.ImageId,
                        imageUrl = $"/Product/GetImage/{pi.ImageId}"
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} images for product {ProductId}, color {ColorId}",
                    images.Count, productId, colorId);

                return Json(new { success = true, images = images });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting images for product {ProductId}, color {ColorId}", productId, colorId);
                return Json(new { success = false, message = "Lỗi khi tải hình ảnh: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null || image.ImageData == null || string.IsNullOrEmpty(image.ImageMimeType))
            {
                _logger.LogWarning("Image not found or invalid for ID: {ImageId}", id);
                return NotFound();
            }

            _logger.LogInformation("Serving image ID: {ImageId}, Size: {Size} bytes, Type: {MimeType}",
                id, image.ImageData.Length, image.ImageMimeType);
            return File(image.ImageData, image.ImageMimeType);
        }

        [AdminAuthorize]
        [HttpGet]
        public async Task<IActionResult> CreateTest()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
            ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
            ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName");
            return View();
        }

        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTest(Product product)
        {
            _logger.LogInformation("=== CREATE TEST PRODUCT ===");
            _logger.LogInformation("Product: {@Product}", product);
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("ModelState Error - Key: {Key}, Errors: {Errors}",
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }

                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
                ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
                ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName", product.DiscountId);
                return View(product);
            }

            try
            {
                _context.Products.Add(product);
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChanges result: {Result}, Product ID: {ProductId}", result, product.ProductId);

                TempData["Success"] = "Test product created successfully without images!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test product");
                ModelState.AddModelError("", "Error: " + ex.Message);

                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
                ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
                ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName", product.DiscountId);
                return View(product);
            }
        }        [AdminAuthorize(area: "Product", action: "Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Loading product {ProductId} for editing", id);

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Color)
                .Include(p => p.Discount)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", id);
                TempData["Error"] = "Không tìm thấy sản phẩm";
                return RedirectToAction(nameof(Index));
            }
              // Group images by color for the view
            ViewBag.ProductImagesGrouped = product.ProductImages
                .GroupBy(pi => pi.ColorId ?? 0)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
            ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
            ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName", product.DiscountId);

            return View(product);
        }

        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)] // 100MB total limit
        public async Task<IActionResult> Edit(int id, Product product)
        {
            _logger.LogInformation("=== EDIT PRODUCT REQUEST STARTED ===");
            _logger.LogInformation("Product data received: {@Product}", product);

            if (id != product.ProductId)
            {
                _logger.LogWarning("ID mismatch: path ID {PathId} vs model ID {ModelId}", id, product.ProductId);
                TempData["Error"] = "ID sản phẩm không khớp";
                return RedirectToAction(nameof(Index));
            }

            // Extract colors from form
            var colors = new List<int>();
            var imagesByColor = new Dictionary<int, List<IFormFile>>();
            var keepExistingImages = new Dictionary<int, List<int>>();

            // Log form data
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form key: {Key}, Value count: {Count}", key, Request.Form[key].Count);
            }

            // Get all color values first
            if (Request.Form.ContainsKey("Colors"))
            {
                var colorValues = Request.Form["Colors"];
                foreach (var colorValue in colorValues)
                {
                    if (int.TryParse(colorValue, out int colorId) && colorId > 0)
                    {
                        colors.Add(colorId);
                        // Initialize the image list for this color
                        imagesByColor[colorId] = new List<IFormFile>();
                        keepExistingImages[colorId] = new List<int>();
                    }
                }
            }

            // Get the existing images to keep
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("KeepImage_"))
                {
                    var parts = key.Split('_');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[1], out int colorId) &&
                        int.TryParse(parts[2], out int imageId))
                    {
                        if (!keepExistingImages.ContainsKey(colorId))
                        {
                            keepExistingImages[colorId] = new List<int>();
                        }
                        keepExistingImages[colorId].Add(imageId);
                    }
                }
            }

            // Log all files to help with debugging
            _logger.LogInformation("Total files in request: {Count}", Request.Form.Files.Count);
            foreach (var file in Request.Form.Files)
            {
                _logger.LogInformation("File: {Name}, FileName: {FileName}, Size: {Size}",
                    file.Name, file.FileName, file.Length);
            }

            // Group new files by color
            for (int i = 0; i < Request.Form.Files.Count; i++)
            {
                var file = Request.Form.Files[i];
                if (file.Name.StartsWith("NewImages_"))
                {
                    var parts = file.Name.Split('_');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int colorId) && colorId > 0)
                    {
                        if (!imagesByColor.ContainsKey(colorId))
                        {
                            imagesByColor[colorId] = new List<IFormFile>();
                        }
                        imagesByColor[colorId].Add(file);
                        _logger.LogInformation("Added new file {FileName} to color {ColorId}",
                            file.FileName, colorId);
                    }
                }
            }

            async Task<IActionResult> PrepareViewBagsAndReturn()
            {
                var existingProduct = await _context.Products
                    .Include(p => p.ProductImages)
                        .ThenInclude(pi => pi.Color)
                    .FirstOrDefaultAsync(p => p.ProductId == id);
                  // Group images by color for the view
                ViewBag.ProductImagesGrouped = existingProduct?.ProductImages
                    .GroupBy(pi => pi.ColorId ?? 0)
                    .ToDictionary(g => g.Key, g => g.ToList()) ?? new Dictionary<int, List<ProductImage>>();

                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", product.CategoryId);
                ViewBag.Colors = new SelectList(await _context.Colors.ToListAsync(), "ColorId", "ColorName");
                ViewBag.Discounts = new SelectList(await _context.DiscountPrograms.ToListAsync(), "DiscountId", "DiscountName", product.DiscountId);
                return View(product);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                return await PrepareViewBagsAndReturn();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the existing product with images
                var existingProduct = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (existingProduct == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found during edit", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction(nameof(Index));
                }

                // Update basic product information
                existingProduct.ProductName = product.ProductName;
                existingProduct.ShortDescription = product.ShortDescription;
                existingProduct.DetailDescription = product.DetailDescription;
                existingProduct.Price = product.Price;
                existingProduct.Stock = product.Stock;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.DiscountId = product.DiscountId;

                // Save the updated product
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated basic product information for ID: {ProductId}", id);
                  // Remove images that weren't marked to keep
                var imagesToRemove = existingProduct.ProductImages
                    .Where(pi => !keepExistingImages.ContainsKey(pi.ColorId ?? 0) ||
                                !keepExistingImages[pi.ColorId ?? 0].Contains(pi.ImageId))
                    .ToList();

                if (imagesToRemove.Any())
                {
                    _logger.LogInformation("Removing {Count} images for product {ProductId}",
                        imagesToRemove.Count, id);
                    _context.ProductImages.RemoveRange(imagesToRemove);
                    await _context.SaveChangesAsync();
                }

                // Add new images for each color
                foreach (var colorId in colors)
                {
                    var newImages = imagesByColor[colorId];
                    if (newImages.Any())
                    {
                        _logger.LogInformation("Adding {Count} new images for color {ColorId}",
                            newImages.Count, colorId);

                        foreach (var image in newImages.Where(i => i != null && i.Length > 0))
                        {
                            try
                            {
                                var imageData = await _imageService.ProcessImageAsync(image);
                                if (imageData == null)
                                {
                                    _logger.LogWarning("Image processing returned null for {FileName}", image.FileName);
                                    continue;
                                }

                                var productImage = new ProductImage
                                {
                                    ProductId = id,
                                    ColorId = colorId,
                                    ImageData = imageData,
                                    ImageMimeType = "image/jpeg"
                                };

                                _context.ProductImages.Add(productImage);
                                await _context.SaveChangesAsync();

                                _logger.LogInformation(
                                    "Added new image for product {ProductId}, color {ColorId}, size {Size} bytes",
                                    id, colorId, imageData.Length);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing new image for product {ProductId}", id);
                                ModelState.AddModelError("", $"Lỗi xử lý ảnh: {ex.Message}");
                                await transaction.RollbackAsync();
                                return await PrepareViewBagsAndReturn();
                            }
                        }
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully completed product edit for {ProductId}", id);
                TempData["Success"] = "Cập nhật sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing product: {Message}", ex.Message);
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
                return await PrepareViewBagsAndReturn();
            }
        }        [AdminAuthorize]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Color)
                .Include(p => p.Discount)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            _logger.LogInformation("Starting toggle publish for product ID: {ProductId}", id);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {ProductId}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction(nameof(Index));
                }

                // Toggle publish status
                product.IsPublished = !product.IsPublished;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string action = product.IsPublished ? "hiển thị" : "ẩn";
                _logger.LogInformation("Successfully toggled publish status for product ID: {ProductId} to {Status}",
                    id, product.IsPublished);

                TempData["Success"] = $"Đã {action} sản phẩm '{product.ProductName}' thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling publish status for product ID: {ProductId}", id);
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            _logger.LogInformation("Starting publish for product ID: {ProductId}", id);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {ProductId}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction(nameof(Index));
                }

                if (product.IsPublished)
                {
                    TempData["Info"] = $"Sản phẩm '{product.ProductName}' đã được hiển thị rồi";
                    return RedirectToAction(nameof(Index));
                }

                product.IsPublished = true;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully published product ID: {ProductId}", id);
                TempData["Success"] = $"Đã hiển thị sản phẩm '{product.ProductName}' thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing product ID: {ProductId}", id);
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi hiển thị sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id)
        {
            _logger.LogInformation("Starting unpublish for product ID: {ProductId}", id);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {ProductId}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction(nameof(Index));
                }

                if (!product.IsPublished)
                {
                    TempData["Info"] = $"Sản phẩm '{product.ProductName}' đã được ẩn rồi";
                    return RedirectToAction(nameof(Index));
                }

                product.IsPublished = false;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully unpublished product ID: {ProductId}", id);
                TempData["Success"] = $"Đã ẩn sản phẩm '{product.ProductName}' thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing product ID: {ProductId}", id);
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi ẩn sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
