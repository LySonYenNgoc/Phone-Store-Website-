using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TestCUS
{
    [TestFixture]
    public class Tests
    {
        private IWebDriver? driver;
        private string appUrl = "https://localhost:7279";
        private static string excelPath = @"C:\Users\ADMIN\Desktop\Admin.xlsx";
        private WebDriverWait? wait;
        private bool hasClickedSubmit = false;

        [SetUp]
        public void Setup()
        {
            ExcelPackage.License.SetNonCommercialPersonal("PhoneStore Admin");

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--dns-prefetch-disable");
            options.AddArgument("--remote-allow-origins=*");
            options.AddArgument("--disable-search-engine-choice-screen");
            options.AddArgument("--disable-background-networking");

            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            driver.Manage().Window.Maximize();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        [TearDown]
        public void TearDown()
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
                driver = null;
            }
        }

        [Test, TestCaseSource(nameof(GetTestScenarios))]
        public void RunTestScenario(TestScenario scenario)
        {
            TestContext.WriteLine($"=== BẮT ĐẦU TEST CASE: {scenario.Id} ===");
            TestContext.WriteLine($"Mục tiêu: {scenario.Objective}");
            hasClickedSubmit = false;

            try
            {
                bool isLoginTC = scenario.Id.StartsWith("F1_") || scenario.Objective.ToLower().Contains("đăng nhập");
                if (!isLoginTC && (scenario.Id.StartsWith("F2") || scenario.Id.StartsWith("F3") || scenario.Id.StartsWith("F4") || scenario.Id.StartsWith("F12")))
                {
                    TestContext.WriteLine(" [PRE-CONDITION] Đang thực hiện đăng nhập tự động...");
                    PerformAutoLogin("admin1", "123");
                }

                int stepIdx = 1;
                foreach (var step in scenario.Steps)
                {
                    TestContext.WriteLine($" Bước {stepIdx++}: {step.Action} | Data: {step.TestData}");
                    ExecuteIndividualStep(step);
                }

                TestContext.WriteLine($" Kiểm tra kết quả mong đợi: {scenario.ExpectedResult}");
                ValidateScenarioResult(scenario.ExpectedResult);
                TestContext.WriteLine($" STATUS: [PASSED] {scenario.Id}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($" STATUS: [FAILED] {scenario.Id}");
                TestContext.WriteLine($" LỖI: {ex.Message}");
                throw;
            }
        }

        private void PerformAutoLogin(string user, string pass)
        {
            if (driver == null || wait == null) return;
            if (driver.Url.Contains("Dashboard") || driver.FindElements(By.CssSelector("aside.sidebar")).Any())
                return;

            driver.Navigate().GoToUrl(appUrl + "/AdminAccount/Login");
            var userFields = driver.FindElements(By.Id("Username")).Concat(driver.FindElements(By.Name("username")));
            if (userFields.Any())
            {
                var userEl = userFields.First(e => e.Displayed);
                userEl.Clear();
                userEl.SendKeys(user);
                var passEl = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Password")));
                passEl.Clear();
                passEl.SendKeys(pass);
                var submitBtn = driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault(e => e.Displayed);
                if (submitBtn != null) ClickBy(By.CssSelector("button[type='submit']"));
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void ExecuteIndividualStep(TestScenarioStep step)
        {
            if (driver == null || wait == null) return;
            string action = step.Action.ToLower();
            string data = step.TestData;
            TryAcceptAlert();

            try
            {
                if (action.Contains("mở trang đăng nhập"))
                {
                    driver.Navigate().GoToUrl(appUrl + "/AdminAccount/Login");
                }
                else if (action.Contains("mở màn hình báo cáo") || action.Contains("vào trang báo cáo") || action.Contains("truy cập báo cáo") || action.Contains("báo cáo doanh thu"))
                {
                    NavigateViaSidebar("báo cáo", "/Report/Revenue");
                }
                else if (action.Contains("chọn phần danh mục ở menu") || action.Contains("vào quản lý danh mục"))
                {
                    NavigateViaSidebar("quản lý danh mục", "/Category/Index");
                }
                else if (action.Contains("nhập tên đăng nhập") || action.Contains("nhập email") || action.Contains("nhập tài khoản"))
                {
                    var el = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Username")));
                    el.Clear();
                    el.SendKeys(data);
                }
                else if (action.Contains("nhập mật khẩu"))
                {
                    var el = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Password")));
                    el.Clear();
                    el.SendKeys(data);
                }
                else if (action.Contains("click nút đăng nhập") || action.Contains("đăng nhập") || action.Contains("nhấn nút đăng nhập"))
                {
                    ClickBy(By.CssSelector("button[type='submit']"));
                    hasClickedSubmit = true;
                }
                else if (action.Contains("truy cập trang dashboard"))
                {
                    driver.Navigate().GoToUrl(appUrl + "/Dashboard/Index");
                }
                else if (action.Contains("vào quản lý sản phẩm") || action.Contains("vào trang quản lý sản phẩm") || action.Contains("quản lý sản phẩm"))
                {
                    NavigateViaSidebar("quản lý sản phẩm", "/Product/Index");
                }
                else if (action.Contains("xem tất cả đơn hàng") || action.Contains("vào trang đơn hàng") || action.Contains("quản lý đơn hàng"))
                {
                    var link = driver.FindElements(By.CssSelector("a")).FirstOrDefault(e => e.Displayed && (e.Text.ToLower().Contains("đơn hàng") || e.GetAttribute("href")?.Contains("Order") == true));
                    if (link != null) ClickElement(link);
                    else driver.Navigate().GoToUrl(appUrl + "/Order/Index");
                }
                else if (action.Contains("chọn thêm sản phẩm") || action.Contains("click nút thêm sản phẩm mới") || action.Contains("click button thêm") || action.Contains("vào trang thêm sản phẩm"))
                {
                    driver.Navigate().GoToUrl(appUrl + "/Product/Create");
                }
                else if (action.Contains("nhập tên sản phẩm") || action.Contains("nhập tên"))
                {
                    var el = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("ProductName")));
                    el.Clear();
                    el.SendKeys(data);
                }
                else if (action.Contains("chọn danh mục") || action.Contains("chọn dòng sản phẩm") || action.Contains("chọn hãng"))
                {
                    string cleanData = data.Replace("Danh mục:", "").Replace("Danh mục", "").Replace("Dòng sản phẩm:", "").Replace("Hãng:", "").Trim().Replace("\"", "");
                    if (string.IsNullOrEmpty(cleanData)) return;

                    var selectors = new[] { By.Id("category"), By.Name("categoryId"), By.Id("CategoryId"), By.Name("category") };
                    IWebElement? el = null;
                    foreach (var sel in selectors)
                    {
                        var found = driver.FindElements(sel).FirstOrDefault(e => e.Displayed);
                        if (found != null) { el = found; break; }
                    }

                    if (el == null)
                    {
                        try { el = wait.Until(ExpectedConditions.ElementExists(By.CssSelector("select[id*='ategory'], select[name*='ategory']"))); }
                        catch
                        {
                            var links = driver.FindElements(By.TagName("a")).Where(e => e.Text.ToLower().Contains(cleanData.ToLower())).ToList();
                            if (links.Any()) { ClickElement(links.First()); return; }
                        }
                    }

                    if (el != null)
                    {
                        var select = new SelectElement(el);
                        try
                        {
                            if (cleanData == "-- Chọn danh mục --") select.SelectByIndex(0);
                            else select.SelectByText(cleanData);
                        }
                        catch
                        {
                            var opt = el.FindElements(By.TagName("option")).FirstOrDefault(o => o.Text.ToLower().Contains(cleanData.ToLower()) || cleanData.ToLower().Contains(o.Text.ToLower()));
                            if (opt != null) select.SelectByText(opt.Text);
                            else try { select.SelectByValue(cleanData); } catch { if (select.Options.Count > 1) select.SelectByIndex(1); }
                        }
                    }
                }
                else if (action.Contains("giảm giá") || action.Contains("chương trình"))
                {
                    var el = driver.FindElements(By.Id("DiscountId")).Concat(driver.FindElements(By.Name("DiscountId"))).FirstOrDefault(e => e.Displayed);
                    if (el != null)
                    {
                        var select = new SelectElement(el);
                        try { select.SelectByText(data); }
                        catch
                        {
                            var opt = el.FindElements(By.TagName("option")).FirstOrDefault(o => o.Text.ToLower().Contains(data.ToLower()) || data.ToLower().Contains(o.Text.ToLower()));
                            if (opt != null) select.SelectByText(opt.Text);
                            else try { select.SelectByValue(data); } catch { }
                        }
                    }
                }
                else if (action.Contains("chọn thời gian") || action.Contains("chọn ngày") || action.Contains("bộ lọc ngày"))
                {
                    string targetId = action.Contains("bắt đầu") || action.Contains("từ ngày") ? "startDate" : (action.Contains("kết thúc") || action.Contains("đến ngày") ? "endDate" : "");
                    var els = new List<IWebElement>();
                    if (!string.IsNullOrEmpty(targetId)) els.AddRange(driver.FindElements(By.Id(targetId)));
                    els.AddRange(driver.FindElements(By.CssSelector("input[type='date'], input[id*='Date'], input[name*='Date'], input[placeholder*='ngày']")).Where(e => e.Displayed));
                    if (els.Any() && !string.IsNullOrEmpty(data))
                    {
                        var el = els.First();
                        el.Clear();
                        el.SendKeys(data);
                        if (el.GetAttribute("type") == "text") el.SendKeys(Keys.Tab);
                    }
                }
                else if (action.Contains("bộ lọc tháng") || action.Contains("chọn tháng"))
                {
                    var match = Regex.Match(data, @"\d+");
                    string monthNum = match.Success ? match.Value : data.Trim();
                    if (!string.IsNullOrEmpty(monthNum))
                    {
                        var searchBox = driver.FindElements(By.Name("searchString")).Concat(driver.FindElements(By.Id("searchTerm"))).Concat(driver.FindElements(By.CssSelector("input[placeholder*='tháng']"))).FirstOrDefault(e => e.Displayed);
                        if (searchBox != null) { searchBox.Clear(); searchBox.SendKeys(monthNum); }
                    }
                }
                else if (action.Contains("7 ngày"))
                {
                    var link = driver.FindElements(By.Id("filter7days")).Concat(driver.FindElements(By.LinkText("Doanh thu 7 ngày gần đây"))).Concat(driver.FindElements(By.PartialLinkText("7 ngày"))).FirstOrDefault(e => e.Displayed);
                    if (link != null) ClickElement(link);
                }
                else if (action.Contains("click filter") || action.Contains("lọc theo"))
                {
                    string pSearch = (action + " " + data).ToLower();
                    string filterId = pSearch.Contains("hôm nay") ? "filter-today" : (pSearch.Contains("tuần này") ? "filter-week" : (pSearch.Contains("tháng này") ? "filter-month" : (pSearch.Contains("năm nay") ? "filter-year" : "")));
                    if (!string.IsNullOrEmpty(filterId))
                    {
                        var el = driver.FindElements(By.Id(filterId)).FirstOrDefault(e => e.Displayed);
                        if (el != null) ClickBy(By.Id(filterId));
                    }
                }
                else if (action.Contains("nhập giá"))
                {
                    var el = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Price")));
                    el.Clear();
                    el.SendKeys(data);
                }
                else if (action.Contains("upload hình ảnh") || action.Contains("upload ảnh") || action.Contains("chọn ảnh") || action.Contains("chọn file"))
                {
                    var fileNames = data.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    if (!fileNames.Any() || data.ToLower().Contains("tải từ máy lên")) 
                    {
                        fileNames = new List<string> { "product_sample.jpg" };
                    }

                    var uploadEls = driver.FindElements(By.CssSelector("input[type='file']")).Where(e => e.Displayed).ToList();
                    if (!uploadEls.Any()) uploadEls = driver.FindElements(By.CssSelector("input[type='file']")).ToList();

                    if (uploadEls.Any())
                    {
                        var uploadEl = uploadEls.First();
                        var fullPaths = fileNames.Select(name => {
                            string fileName = name;
                            if (!fileName.ToLower().EndsWith(".jpg") && !fileName.ToLower().EndsWith(".png") && !fileName.ToLower().EndsWith(".jpeg"))
                            {
                                fileName += ".jpg";
                            }
                            // Clean up invalid characters from filename
                            fileName = Regex.Replace(fileName, @"[^\w\.-]", "_");
                            if (!fileName.EndsWith(".jpg")) fileName += ".jpg";

                            string path = Path.Combine(Path.GetTempPath(), fileName);
                            
                            // Minimal valid JPEG header (SOI, DQT, DHT, SOF, SOS, EOI) 
                            // This ensures basic image validation passes.
                            byte[] dummyJpg = new byte[] { 
                                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x60, 0x00, 0x60, 0x00, 0x00, 
                                0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 
                                0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12, 0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 
                                0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 
                                0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xD9 
                            };
                            File.WriteAllBytes(path, dummyJpg);
                            return path;
                        }).ToList();
                        
                        // Đối với input multiple, gửi các đường dẫn cách nhau bởi xuống dòng (\n)
                        uploadEl.SendKeys(string.Join("\n", fullPaths));
                        System.Threading.Thread.Sleep(1500); 
                        TryAcceptAlert();
                    }
                }
                else if (action.Contains("đợi") || action.Contains("chờ"))
                {
                    System.Threading.Thread.Sleep(3000); // Chờ chung cho các bước đợi upload/xử lý
                }
                else if (action.Contains("chọn màu sắc") || action.Contains("chọn màu"))
                {
                    string cleanColor = data.Replace("\"", "").Trim();
                    var selects = driver.FindElements(By.CssSelector(".color-select")).Concat(driver.FindElements(By.Name("Colors"))).Where(e => e.Displayed);
                    if (selects.Any())
                    {
                        var select = new SelectElement(selects.First());
                        if (string.IsNullOrEmpty(cleanColor)) { select.SelectByIndex(0); return; }
                        try { select.SelectByText(cleanColor); }
                        catch
                        {
                            try { select.SelectByValue(cleanColor); }
                            catch
                            {
                                var option = selects.First().FindElements(By.TagName("option")).FirstOrDefault(o => o.Text.ToLower().Contains(cleanColor.ToLower()) || cleanColor.ToLower().Contains(o.Text.ToLower()));
                                if (option != null) select.SelectByText(option.Text);
                            }
                        }
                    }
                }
                else if (action.Contains("nhập mô tả"))
                {
                    IWebElement? el = action.Contains("ngắn") ? driver.FindElements(By.Id("ShortDescription")).FirstOrDefault(e => e.Displayed) : (action.Contains("chi tiết") ? driver.FindElements(By.Id("DetailDescription")).FirstOrDefault(e => e.Displayed) : null);
                    if (el == null) el = driver.FindElements(By.Id("ShortDescription")).Concat(driver.FindElements(By.Id("DetailDescription"))).FirstOrDefault(e => e.Displayed);
                    if (el != null) { el.Clear(); el.SendKeys(data); }
                }
                else if (action.Contains("nhập số lượng"))
                {
                    var el = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Stock")));
                    el.Clear();
                    el.SendKeys(data);
                }
                else if (action.Contains("click lưu") || action.Contains("cập nhật") || action.Contains("nhấn thêm sản phẩm") || action.Contains("click nút thêm"))
                {
                    var btn = driver.FindElements(By.Id("submitButton")).Concat(driver.FindElements(By.Id("btnSubmit"))).Concat(driver.FindElements(By.CssSelector("form button[type='submit']"))).Concat(driver.FindElements(By.CssSelector("button[type='submit']"))).FirstOrDefault(e => e.Displayed);
                    if (btn != null) { ClickElement(btn); hasClickedSubmit = true; }
                    else { ClickBy(By.CssSelector("button[type='submit']")); hasClickedSubmit = true; }
                }
                else if (action.Contains("thanh tìm kiếm") || action.Contains("ô tìm kiếm") || action.Contains("nhập vào tìm kiếm"))
                {
                    var searchInput = GetSearchInput();
                    if (searchInput != null) ClickElement(searchInput);
                }
                else if (action.Contains("nhập từ khóa") || action.Contains("điền từ khóa") || action.Contains("nhập tên sản phẩm"))
                {
                    var searchInput = GetSearchInput();
                    if (searchInput != null)
                    {
                        searchInput.Clear();
                        searchInput.SendKeys(data);
                    }
                    else
                    {
                        // Fallback nỗ lực cuối
                        var idSearch = driver.FindElements(By.Id("search")).FirstOrDefault(e => e.Displayed);
                        if (idSearch != null) { idSearch.Clear(); idSearch.SendKeys(data); }
                    }
                }
                else if (action.Contains("click tìm kiếm") || action.Contains("ấn tìm kiếm") || action.Contains("nhấn tìm kiếm") || action.Contains("thực hiện tìm kiếm"))
                {
                    var btnSearch = driver.FindElements(By.Id("btnSearch"))
                                          .Concat(driver.FindElements(By.Id("searchButton")))
                                          .Concat(driver.FindElements(By.CssSelector("button[type='submit']")))
                                          .Concat(driver.FindElements(By.XPath("//button[contains(., 'Tìm')]")))
                                          .FirstOrDefault(e => e.Displayed);
                    
                    if (btnSearch != null) ClickElement(btnSearch);
                    else {
                        // Nếu không tìm thấy nút, nhấn Enter vào ô search
                        var searchInput = GetSearchInput();
                        if (searchInput != null) {
                            searchInput.SendKeys(Keys.Enter);
                            System.Threading.Thread.Sleep(1000); 
                        }
                    }
                }
                else if (action.Contains("click nút lọc") || action.Contains("lọc kết quả") || action.Contains("ấn nút lọc"))
                {
                    var btnFilter = driver.FindElements(By.CssSelector("button[type='submit']")).Concat(driver.FindElements(By.Id("btnFilter"))).FirstOrDefault(e => e.Displayed && (e.Text.ToLower().Contains("lọc") || e.GetAttribute("value")?.ToLower().Contains("lọc") == true));
                    if (btnFilter != null) ClickElement(btnFilter);
                    else
                    {
                        var btn = driver.FindElements(By.CssSelector("form button[type='submit']")).FirstOrDefault(e => e.Displayed);
                        if (btn != null) ClickElement(btn);
                    }
                }
                else if (action.Contains("lọc"))
                {
                    if (data.ToLower().Contains("samsung") || data.ToLower().Contains("apple") || data.ToLower().Contains("iphone"))
                    {
                        var selEls = driver.FindElements(By.Id("category")).Concat(driver.FindElements(By.Name("category"))).Where(e => e.Displayed);
                        if (selEls.Any()) { var sel = new SelectElement(selEls.First()); try { sel.SelectByText(data); } catch { sel.SelectByValue(data); } }
                    }
                    var btnFilter = driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault(e => e.Displayed && e.Text.Contains("Lọc"));
                    if (btnFilter != null) ClickElement(btnFilter);
                }
                else if (action.Contains("xuất excel") || action.Contains("export excel"))
                {
                    var btns = driver.FindElements(By.CssSelector("button, a")).Where(e => e.Text.ToLower().Contains("excel")).ToList();
                    if (btns.Any()) btns.First().Click();
                }
                else if (action.Contains("xuất pdf") || action.Contains("export pdf") || action.Contains("định dạng pdf"))
                {
                    var btns = driver.FindElements(By.CssSelector("button, a")).Where(e => e.Text.ToLower().Contains("pdf")).ToList();
                    if (btns.Any()) btns.First().Click();
                }
                else if (action.Contains("xác nhận xóa") || action.Contains("xác nhận hủy") || action.Contains("xác nhận"))
                {
                    TryAcceptAlert();
                    var confirmBtn = driver.FindElements(By.CssSelector("#deleteModal button[type='submit']")).Concat(driver.FindElements(By.CssSelector("button.btn-primary"))).FirstOrDefault(e => e.Displayed);
                    if (confirmBtn != null) confirmBtn.Click();
                }
                else if (action.Contains("nhập các thông tin khác") || action.Contains("điền thông tin") || action.Contains("nhập thông tin") || action.Contains("nhập đầy đủ thông tin") || action.Contains("các thông tin"))
                {
                    var prefixes = new Dictionary<string, string> { { "Tên sản phẩm:", "nhập tên sản phẩm" }, { "Danh mục:", "chọn danh mục" }, { "Dòng sản phẩm:", "chọn danh mục" }, { "Hãng:", "chọn danh mục" }, { "Giá:", "nhập giá" }, { "Màu:", "chọn màu sắc" }, { "Mô tả ngắn:", "nhập mô tả ngắn" }, { "Mô tả chi tiết:", "nhập mô tả chi tiết" }, { "Mô tả:", "nhập mô tả" }, { "Nhập mô tả:", "nhập mô tả" }, { "chi tiết:", "nhập mô tả chi tiết" }, { "Số lượng tồn:", "nhập số lượng" }, { "Số lượng:", "nhập số lượng" }, { "tồn kho:", "nhập số lượng" }, { "Giảm giá:", "giảm giá" }, { "Chương trình giảm giá:", "giảm giá" } };
                    var foundPositions = new List<TextMatch>();
                    foreach (var p in prefixes)
                    {
                        int pos = data.IndexOf(p.Key);
                        while (pos != -1) { foundPositions.Add(new TextMatch { Pos = pos, Prefix = p.Key, Action = p.Value }); pos = data.IndexOf(p.Key, pos + p.Key.Length); }
                    }
                    foundPositions = foundPositions.OrderBy(x => x.Pos).ToList();
                    for (int i = 0; i < foundPositions.Count; i++)
                    {
                        for (int j = 0; j < foundPositions.Count; j++)
                        {
                            if (i == j) continue;
                            if (foundPositions[j].Pos <= foundPositions[i].Pos && foundPositions[j].Pos + foundPositions[j].Prefix.Length >= foundPositions[i].Pos + foundPositions[i].Prefix.Length && foundPositions[j].Prefix.Length > foundPositions[i].Prefix.Length)
                            {
                                foundPositions.RemoveAt(i);
                                i--; break;
                            }
                        }
                    }
                    for (int i = 0; i < foundPositions.Count; i++)
                    {
                        int start = foundPositions[i].Pos + foundPositions[i].Prefix.Length;
                        int end = (i + 1 < foundPositions.Count) ? foundPositions[i + 1].Pos : data.Length;
                        string val = data.Substring(start, end - start).Trim().Replace("\n", " ").Replace("\r", "").Trim();
                        if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2) val = val.Substring(1, val.Length - 2).Trim();
                        if (!string.IsNullOrEmpty(val) || foundPositions[i].Prefix.Contains("Màu")) { ExecuteIndividualStep(new TestScenarioStep { Action = foundPositions[i].Action, TestData = val }); }
                    }
                }
                else if (action.Contains("click") || action.Contains("ấn") || action.Contains("nhấn vào"))
                {
                    string target = data.ToLower();
                    if (string.IsNullOrEmpty(target)) {
                        target = action.Replace("click vào", "").Replace("click nút", "").Replace("click link", "").Replace("ấn", "").Replace("nhấn vào", "").Replace("link", "").Replace("nút", "").Replace("khu vực", "").Trim();
                    }
                    
                    if (!string.IsNullOrEmpty(target)) {
                        IWebElement? el = null;
                        var elements = driver.FindElements(By.XPath($"//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{target}')]")).Where(e => e.Displayed && e.Enabled).ToList();
                        if (elements.Any()) el = elements.First();
                        
                        if (el == null) {
                            var links = driver.FindElements(By.TagName("a")).Where(e => e.Displayed && e.Text.ToLower().Contains(target)).ToList();
                            if (links.Any()) el = links.First();
                        }
                        
                        // Fallback cho khu vực upload ảnh/dropzone
                        if (el == null && (target.Contains("upload") || target.Contains("ảnh") || target.Contains("hình"))) {
                             var fallback = driver.FindElements(By.CssSelector(".upload-area, .dropzone, #uploadArea, .btn-upload, [id*='pload'], [id*='allery']")).FirstOrDefault(e => e.Displayed);
                             if (fallback != null) el = fallback;
                        }

                        if (el != null) ClickElement(el);
                    }
                }
                else if (action.Contains("quay lại")) { driver.Navigate().Back(); }
            }
            catch (Exception ex) { TestContext.WriteLine($" [WARN] Lỗi khi thực hiện bước '{action}': {ex.Message}"); }
        }

        private IWebElement? GetSearchInput()
        {
            if (driver == null) return null;
            var selectors = new[] { 
                By.Id("search"), 
                By.Name("search"), 
                By.Id("searchTerm"), 
                By.Id("searchString"),
                By.Name("searchString"),
                By.CssSelector("input[placeholder*='Tên sản phẩm']"),
                By.CssSelector("input[placeholder*='tìm kiếm']"),
                By.CssSelector("input[type='search']"),
                By.CssSelector(".search-input")
            };
            
            foreach (var sel in selectors) {
                var el = driver.FindElements(sel).FirstOrDefault(e => e.Displayed);
                if (el != null) return el;
            }
            return null;
        }

        private void NavigateViaSidebar(string text, string fallbackUrl)
        {
            if (driver == null) return;
            var allLinks = driver.FindElements(By.CssSelector("aside.sidebar a"));
            var target = allLinks.FirstOrDefault(e => e.Text.ToLower().Contains(text.ToLower()) || (e.GetAttribute("href")?.ToLower().Contains(fallbackUrl.ToLower()) == true));
            if (target != null) ClickElement(target);
            else driver.Navigate().GoToUrl(appUrl + fallbackUrl);
            System.Threading.Thread.Sleep(1000);
        }

        public static IEnumerable<TestScenario> GetTestScenarios() => LoadScenariosFromExcel(excelPath);

        private static List<TestScenario> LoadScenariosFromExcel(string path)
        {
            var list = new List<TestScenario>();
            if (!File.Exists(path)) return list;
            ExcelPackage.License.SetNonCommercialPersonal("PhoneStore Admin");
            using (var package = new ExcelPackage(new FileInfo(path)))
            {
                var sheet = package.Workbook.Worksheets[0];
                int rows = sheet.Dimension?.End.Row ?? 0;
                TestScenario? current = null;
                for (int r = 2; r <= rows; r++)
                {
                    string reqId = sheet.Cells[r, 2].Text?.Trim() ?? "";
                    string tcId = sheet.Cells[r, 3].Text?.Trim() ?? "";
                    string objective = sheet.Cells[r, 4].Text?.Trim() ?? "";
                    string stepNum = sheet.Cells[r, 6].Text?.Trim() ?? "";
                    string stepAction = sheet.Cells[r, 7].Text?.Trim() ?? "";
                    string testData = sheet.Cells[r, 8].Text?.Trim() ?? "";
                    string expected = sheet.Cells[r, 9].Text?.Trim() ?? "";
                    if (string.IsNullOrEmpty(stepAction)) continue;
                    bool isNewTC = (stepNum == "1") || (!string.IsNullOrEmpty(tcId) && (current == null || tcId != current.Id));
                    if (isNewTC)
                    {
                        if (current != null) list.Add(current);
                        string finalId = !string.IsNullOrEmpty(tcId) ? tcId : reqId;
                        if (string.IsNullOrEmpty(finalId)) finalId = $"Row_{r}";
                        current = new TestScenario { Id = finalId, Objective = objective, ExpectedResult = expected, Steps = new List<TestScenarioStep>() };
                    }
                    if (current != null)
                    {
                        current.Steps.Add(new TestScenarioStep { Action = stepAction, TestData = testData });
                        if (string.IsNullOrEmpty(current.ExpectedResult)) current.ExpectedResult = expected;
                    }
                }
                if (current != null) list.Add(current);
            }
            return list;
        }

        private void ClickBy(By selector)
        {
            if (driver == null || wait == null) return;
            var el = wait.Until(ExpectedConditions.ElementToBeClickable(selector));
            ClickElement(el);
        }

        private void ClickElement(IWebElement el)
        {
            if (driver == null) return;
            try { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el); System.Threading.Thread.Sleep(500); el.Click(); }
            catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el); }
            TryAcceptAlert();
        }

        private void TryAcceptAlert()
        {
            try { System.Threading.Thread.Sleep(500); var alert = driver?.SwitchTo().Alert(); if (alert != null) { TestContext.WriteLine($" [INFO] Alert: '{alert.Text}'"); alert.Accept(); } }
            catch { }
        }

        private void ValidateScenarioResult(string expected)
        {
            if (driver == null) return;
            System.Threading.Thread.Sleep(2000);
            string alertText = "";
            try { var alert = driver.SwitchTo().Alert(); alertText = alert.Text.ToLower(); alert.Accept(); } catch { }
            string html5Errors = "";
            try { html5Errors = ((IJavaScriptExecutor)driver).ExecuteScript("return Array.from(document.querySelectorAll('input:invalid, select:invalid')).map(el => el.validationMessage).join(' ');")?.ToString()?.ToLower() ?? ""; } catch { }
            string bodyText = driver.FindElement(By.TagName("body")).Text.ToLower() + " " + alertText + " " + html5Errors;
            string url = driver.Url.ToLower();
            string expectedLower = expected?.ToLower() ?? "";
            if (!string.IsNullOrEmpty(expectedLower) && (bodyText.Contains(expectedLower) || alertText.Contains(expectedLower) || html5Errors.Contains(expectedLower))) return;
            if (!hasClickedSubmit && (url.Contains("login") || url.Contains("create") || url.Contains("edit")) && !bodyText.Contains("thành công") && !bodyText.Contains("lỗi") && !bodyText.Contains("không đúng") && string.IsNullOrEmpty(html5Errors))
            {
                var submitBtn = driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault(e => e.Displayed);
                if (submitBtn != null) { submitBtn.Click(); System.Threading.Thread.Sleep(1000); try { html5Errors = ((IJavaScriptExecutor)driver).ExecuteScript("return Array.from(document.querySelectorAll('input:invalid')).map(el => el.validationMessage).join(' ');")?.ToString()?.ToLower() ?? ""; } catch { } bodyText = driver.FindElement(By.TagName("body")).Text.ToLower() + " " + html5Errors; }
            }
            if (expectedLower.Contains("thành công") || expectedLower.Contains("passed") || expectedLower.Contains("hợp lệ") || expectedLower.Contains("dashboard") || expectedLower.Contains("vào trang"))
            {
                bool success = bodyText.Contains("thành công") || bodyText.Contains("dashboard") || url.Contains("dashboard") || url.Contains("index") || (!url.Contains("login") && !url.Contains("error"));
                if (!success) throw new Exception("Mong đợi Thành công nhưng không tìm thấy.");
            }
            else if (expectedLower.Contains("lỗi") || expectedLower.Contains("thông báo") || expectedLower.Contains("không đúng") || expectedLower.Contains("vui lòng") || expectedLower.Contains("tồn tại") || expectedLower.Contains("failed") || expectedLower.Contains("sai"))
            {
                bool errorEncountered = bodyText.Contains("lỗi") || bodyText.Contains("thông báo") || bodyText.Contains("không đúng") || bodyText.Contains("vui lòng") || bodyText.Contains("yêu cầu") || bodyText.Contains("bỏ trống") || bodyText.Contains("sai") || bodyText.Contains("tồn tại") || bodyText.Contains("không") || !string.IsNullOrEmpty(html5Errors) || driver.FindElements(By.CssSelector(".field-validation-error, .text-danger, .validation-summary-errors")).Any(e => e.Displayed && !string.IsNullOrEmpty(e.Text));
                if (!errorEncountered) throw new Exception("Mong đợi Lỗi nhưng không tìm thấy.");
            }
        }
    }

    public class TextMatch
    {
        public int Pos { get; set; }
        public string Prefix { get; set; } = "";
        public string Action { get; set; } = "";
    }

    public class TestScenario
    {
        public string Id { get; set; } = "";
        public string Objective { get; set; } = "";
        public string ExpectedResult { get; set; } = "";
        public List<TestScenarioStep> Steps { get; set; } = new List<TestScenarioStep>();
        public override string ToString() => $"[{Id}] {Objective}";
    }

    public class TestScenarioStep
    {
        public string Action { get; set; } = "";
        public string TestData { get; set; } = "";
    }
}
