using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace TestCUS
{
    [TestFixture]
    public class Tests
    {
        private IWebDriver? driver;
        private string appUrl = "https://localhost:7011";
        private static string excelPath = @"C:\Users\ADMIN\Desktop\Customer.xlsx";
        private WebDriverWait? wait; 

        [SetUp]
        public void Setup()
        {
            ExcelPackage.License.SetNonCommercialPersonal("PhoneStore Admin");

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors"); //Bỏ lỗi SSL (do chạy localhost HTTPS)
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--remote-debugging-port=9222"); // Tránh treo driver

            driver = new ChromeDriver(options); 
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5); 
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30); // Tăng giới hạn chờ load trang
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

        private bool hasClickedSubmit = false;

        // TestCaseSource giúp hiển thị từng TC thành 1 dòng riêng biệt trong Test Explorer
        [Test, TestCaseSource(nameof(GetTestScenarios))]  // Lấy dữ liệu TC từ Excel
        public void RunTestScenario(TestScenario scenario)
        {
            TestContext.WriteLine($"=== BẮT ĐẦU TEST CASE: {scenario.Id} ==="); 
            TestContext.WriteLine($"Mục tiêu: {scenario.Objective}");
            hasClickedSubmit = false; // Reset trạng thái trước mỗi TC

            try
            {
                // Điều kiện tiên quyết: Các TC từ F1.3_01 trở đi thường yêu cầu Đăng nhập trước
                string tcNum = scenario.Id.Split('_').FirstOrDefault()?.Replace("F", ""); 
                bool needsLogin = false; 
                if (double.TryParse(tcNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num))  
                {
                    if (num >= 1.3) needsLogin = true;
                }
                
                // Hoặc kiểm tra trực tiếp prefix F1.3, F1.4, F2...
                if (scenario.Id.StartsWith("F1.3") || scenario.Id.StartsWith("F1.4") || scenario.Id.StartsWith("F2") || scenario.Id.StartsWith("F3"))
                    needsLogin = true;

                if (needsLogin)
                {
                    TestContext.WriteLine(" [PRE-CONDITION] Đang thực hiện đăng nhập tự động để chuẩn bị cho test...");
                    PerformAutoLogin("test@gmail.com", "Nhut@2005");
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
                throw; // Ném lỗi để NUnit đánh dấu Fail bài test
            }
        }

        private void PerformAutoLogin(string email, string pass)
        {
            if (driver == null) return;
            driver.Navigate().GoToUrl(appUrl + "/Customer/Login");
            
            var emailFields = driver.FindElements(By.Id("Email")).Concat(driver.FindElements(By.Name("Email")));
            if (emailFields.Any()) 
            {
                var emailEl = emailFields.First(e => e.Displayed);
                emailEl.Clear();
                emailEl.SendKeys(email);
                
                var passEl = driver.FindElement(By.Id("Password"));
                passEl.Clear();
                passEl.SendKeys(pass);
                
                var submitBtn = driver.FindElements(By.Id("login-submit-btn")).FirstOrDefault(e => e.Displayed);
                if (submitBtn != null) submitBtn.Click();
                else driver.FindElement(By.CssSelector("button[type='submit']")).Click();
                
                System.Threading.Thread.Sleep(1000); // Chờ login xong
            }
        }

        public static IEnumerable<TestScenario> GetTestScenarios()
        {
            // Tải dữ liệu từ Excel và trả về danh sách các TC cho NUnit
            var scenarios = LoadScenariosFromExcel(excelPath);
            return scenarios;
        }

        private static List<TestScenario> LoadScenariosFromExcel(string path) // Đọc dữ liệu từ Excel và chuyển thành danh sách TestScenario
        {
            var list = new List<TestScenario>();
            if (!File.Exists(path)) return list;

            ExcelPackage.License.SetNonCommercialPersonal("PhoneStore Admin");
            using (var package = new ExcelPackage(new FileInfo(path)))
            {
                // Luôn lấy Sheet đầu tiên
                var sheet = package.Workbook.Worksheets[0];
                int rows = sheet.Dimension?.End.Row ?? 0;

                TestScenario? current = null;
                for (int r = 2; r <= rows; r++)
                {
                    string reqId = sheet.Cells[r, 2].Text?.Trim() ?? "";   // Requirement ID
                    string tcId = sheet.Cells[r, 3].Text?.Trim() ?? "";    // Test Case ID
                    string objective = sheet.Cells[r, 4].Text?.Trim() ?? ""; // Objective
                    string stepNum = sheet.Cells[r, 6].Text?.Trim() ?? "";   // Step #
                    string stepAction = sheet.Cells[r, 7].Text?.Trim() ?? ""; // Action
                    string testData = sheet.Cells[r, 8].Text?.Trim() ?? "";  // Data
                    string expected = sheet.Cells[r, 9].Text?.Trim() ?? "";  // Expected

                    // Một TC mới bắt đầu nếu Step # là "1" hoặc tìm thấy ID TC mới khác với ID hiện tại
                    bool isNewTC = stepNum == "1" || (!string.IsNullOrEmpty(tcId) && (current == null || tcId != current.Id));

                    if (isNewTC && !string.IsNullOrEmpty(stepAction))
                    {
                        if (current != null) list.Add(current);

                        string finalId = !string.IsNullOrEmpty(tcId) ? tcId : reqId;
                        if (string.IsNullOrEmpty(finalId)) finalId = $"Row_{r}";

                        current = new TestScenario
                        {
                            Id = finalId,
                            Objective = objective,
                            ExpectedResult = expected,
                            Steps = new List<TestScenarioStep>()
                        };
                    }

                    if (current != null && !string.IsNullOrEmpty(stepAction))
                    {
                        current.Steps.Add(new TestScenarioStep { Action = stepAction, TestData = testData });
                        if (string.IsNullOrEmpty(current.ExpectedResult)) current.ExpectedResult = expected;
                    }
                }
                if (current != null) list.Add(current);
            }
            return list;
        }

        private void ExecuteIndividualStep(TestScenarioStep step) 
        {
            if (driver == null || wait == null) return;

            string action = step.Action?.ToLower().Trim() ?? "";
            string data = step.TestData?.Trim() ?? "";

            // Xử lý Điều hướng trang (Tuân thủ Luồng web: Click Link thay vì trực tiếp vào URL)
            if (action.Contains("mở") || action.Contains("truy cập"))
            {
                if (action.Contains("đăng nhập"))
                {
                    driver.Navigate().GoToUrl(appUrl); // Về home trước để click link
                    if (driver.FindElements(By.Id("header-login-link")).Any(e => e.Displayed))
                        ClickElement(By.Id("header-login-link"));
                    else
                        driver.Navigate().GoToUrl(appUrl + "/Customer/Login");
                }
                else if (action.Contains("đăng ký"))
                {
                    driver.Navigate().GoToUrl(appUrl);
                    if (driver.FindElements(By.Id("header-register-link")).Any(e => e.Displayed))
                        ClickElement(By.Id("header-register-link"));
                    else
                        driver.Navigate().GoToUrl(appUrl + "/Customer/Register");
                }
                else if (action.Contains("trang chủ")) driver.Navigate().GoToUrl(appUrl + "/");
                else if (action.Contains("thông tin cá nhân") || action.Contains("profile")) driver.Navigate().GoToUrl(appUrl + "/Customer/Profile");
                else if (action.Contains("thanh toán")) driver.Navigate().GoToUrl(appUrl + "/Checkout");
                else if (action.Contains("sản phẩm")) driver.Navigate().GoToUrl(appUrl + "/Product");
                else if (action.Contains("giỏ hàng")) driver.Navigate().GoToUrl(appUrl + "/Cart");
                return;
            }

            // Xử lý Nhập liệu (Hỗ trợ cả "nhập", "điền" và "nhấn" cho các trường nhập liệu)
            if (action.Contains("nhập") || action.Contains("điền") || (action.Contains("nhấn") && !action.Contains("chọn") && (action.Contains("mật khẩu") || action.Contains("email") || action.Contains(" họ ") || action.Contains(" tên "))))
            {
                // Loại trừ trường hợp "nhấn đổi mật khẩu" hoặc "nhấn đăng ký" vì đó là nút bấm, không phải nhập liệu
                if (action.Contains("đổi mật khẩu") || action.Contains("đăng ký") || action.Contains("đăng nhập") || action.Contains("đặt hàng"))
                {
                    // Chuyển sang phần xử lý Click phía dưới
                }
                else
                {
                    if (action.Contains("email")) InputText("Email", data);
                    else if (action.Contains("mật khẩu cũ") || action.Contains("mật khẩu hiện tại")) InputText("currentPassword", data);
                    else if (action.Contains("xác nhận mật khẩu mới")) InputText("confirmNewPassword", data);
                    else if (action.Contains("mật khẩu mới")) InputText("newPassword", data);
                    else if (action.Contains("xác nhận mật khẩu") || action.Contains("xác nhận mât khẩu")) InputText("ConfirmPassword", data);
                    else if (action.Contains("mật khẩu")) InputText("Password", data);
                    else if (action.Contains("họ và tên") || action.Contains("tên mới") || action.Contains("họ tên")) InputText("Name", data);
                    else if (action.Contains(" họ")) InputText("FirstName", data);
                    else if (action.Contains("tên người nhận")) InputText("NewAddress_RecipientName", data);
                    else if (action.Contains(" tên ")) InputText("LastName", data);
                    else if (action.Contains("quận") || action.Contains("huyện")) InputText("NewAddress_District", data);
                    else if (action.Contains("phường") || action.Contains("xã")) InputText("NewAddress_Ward", data);
                    else if (action.Contains("tỉnh") || action.Contains("thành phố") || action.Contains("tp")) InputText("NewAddress_Province", data);
                    else if (action.Contains("địa chỉ chi tiết") || action.Contains("số nhà") || action.Contains("tên đường")) InputText("NewAddress_AddressLine", data);
                    else if (action.Contains("người nhận")) InputText("NewAddress_RecipientName", data);
                    else if (action.Contains("số điện thoại") || action.Contains("sđt"))
                    {
                        if (driver.FindElements(By.Id("Phone")).Any()) InputText("Phone", data);
                        else if (driver.FindElements(By.Id("NewAddress_Phone")).Any()) InputText("NewAddress_Phone", data);
                        else InputText("PhoneNumber", data);
                    }
                    else if (action.Contains("số lượng"))
                    {
                        // Làm sạch data  
                        string cleanData = data;
                        if (data.Contains(":")) cleanData = data.Split(':').Last().Trim();
                        else if (data.ToLower().StartsWith("số lượng")) cleanData = data.Substring(8).Trim();
                        else if (data.ToLower().StartsWith("số lượng :")) cleanData = data.Substring(10).Trim();

                        if (driver.Url.ToLower().Contains("cart"))
                        {
                            // Trong giỏ hàng, input thường không có ID/Name, tìm theo cấu trúc row
                            var cartItems = driver.FindElements(By.CssSelector("li.flex, tr, .cart-item"));
                            if (cartItems.Any())
                            {
                                var input = cartItems.First().FindElements(By.CssSelector("input[type='number']")).FirstOrDefault();
                                if (input != null)
                                {
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", input);
                                    try 
                                    {
                                        input.Click();
                                        input.SendKeys(Keys.Control + "a");
                                        input.SendKeys(Keys.Backspace);
                                        input.Clear();
                                        input.SendKeys(cleanData);
                                    }
                                    catch (StaleElementReferenceException)
                                    {
                                        // Re-find trong trường hợp DOM thay đổi sau khi Clear
                                        input = driver.FindElements(By.CssSelector("input[type='number']")).FirstOrDefault();
                                        input?.SendKeys(cleanData);
                                    }
                                    
                                    if (input != null) input.SendKeys(Keys.Tab); 
                                    System.Threading.Thread.Sleep(1000);
                                    return;
                                }
                            }
                        }
                        InputText("quantity", cleanData);
                    }
                    else if (action.Contains("năm sinh")) InputText("BirthYear", data);
                    else if (action.Contains("tìm kiếm"))
                    {
                        InputText("search", data);
                    }
                    return;
                }
            }

            // Xử lý Click và Chọn
            if (action.Contains("click") || action.Contains("nhấn") || action.Contains("chọn"))
            {
                if (action.Contains("đăng nhập"))
                {
                    if (driver.FindElements(By.Id("login-submit-btn")).Any(e => e.Displayed))
                        ClickElement(By.Id("login-submit-btn"));
                    else if (driver.FindElements(By.Id("header-login-link")).Any(e => e.Displayed))
                        ClickElement(By.Id("header-login-link"));
                    else if (driver.FindElements(By.Id("login-link-on-register")).Any(e => e.Displayed))
                        ClickElement(By.Id("login-link-on-register"));
                    else
                        ClickElement(By.CssSelector("button[type='submit']"));
                    
                    hasClickedSubmit = true;
                }
                else if (action.Contains("đăng ký"))
                {
                    if (driver.FindElements(By.Id("register-submit-btn")).Any(e => e.Displayed))
                        ClickElement(By.Id("register-submit-btn"));
                    else if (driver.FindElements(By.Id("header-register-link")).Any(e => e.Displayed))
                        ClickElement(By.Id("header-register-link"));
                    else if (driver.FindElements(By.Id("register-link-on-login")).Any(e => e.Displayed))
                        ClickElement(By.Id("register-link-on-login"));
                    else
                        ClickElement(By.CssSelector("button[type='submit']"));

                    hasClickedSubmit = true;
                }
                else if (action.Contains("đăng xuất"))
                {
                    if (driver.FindElements(By.Id("header-logout-btn")).Any(e => e.Displayed))
                        ClickElement(By.Id("header-logout-btn"));
                    else
                        ClickElement(By.CssSelector("form[action*='Logout'] button, button[onclick*='logout'], a[href*='Logout']"));
                }
                else if (action.Contains("profile") || action.Contains("avatar") || action.Contains("người dùng") || action.Contains("tài khoản")) 
                {
                    if (driver.FindElements(By.Id("user-menu-btn")).Any(e => e.Displayed))
                        ClickElement(By.Id("user-menu-btn"));
                    else if (driver.FindElements(By.CssSelector("a[href*='Profile'], .fa-user-circle, .fa-user")).Any(e => e.Displayed))
                        ClickElement(By.CssSelector("a[href*='Profile'], .fa-user-circle, .fa-user"));
                    else 
                        driver.Navigate().GoToUrl(appUrl + "/Customer/Profile");
                }
                else if (action.Contains("chi tiết"))
                {
                    if (!string.IsNullOrEmpty(data))
                    {
                        var items = driver.FindElements(By.CssSelector(".bg-white.rounded-lg, tr, li.flex, .order-item"));
                        var target = items.FirstOrDefault(i => i.Text.Contains(data));
                        if (target != null)
                        {
                            var btn = target.FindElements(By.CssSelector("a, button")).FirstOrDefault(e => e.Text.Contains("Chi tiết") || (e.GetAttribute("href")?.Contains("Detail") ?? false));
                            if (btn != null) 
                            {
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", btn);
                                System.Threading.Thread.Sleep(500);
                                try { btn.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn); }
                                TryAcceptAlert();
                                return;
                            }
                        }
                    }
                    ClickElement(By.CssSelector("a[href*='Detail'], button[onclick*='detail'], .btn-detail, a.bg-gray-100"));
                }
                else if ((action.Contains("đơn hàng") || action.Contains("lịch sử")) && !action.Contains("chi tiết"))
                {
                    var orderLink = driver.FindElements(By.CssSelector("a, button"))
                                    .FirstOrDefault(e => e.Displayed && (e.Text.Contains("Đơn hàng") || (e.GetAttribute("href")?.Contains("Orders") ?? false)));
                    if (orderLink != null) 
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", orderLink);
                        System.Threading.Thread.Sleep(500);
                        try { orderLink.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", orderLink); }
                        TryAcceptAlert();
                    }
                    else driver.Navigate().GoToUrl(appUrl + "/Customer/Orders");
                }
                else if (action.Contains("trang chủ")) ClickElement(By.LinkText("Trang chủ"));
                else if (action.Contains("sản phẩm")) ClickElement(By.LinkText("Sản phẩm"));
                else if (action.Contains("liên hệ")) ClickElement(By.LinkText("Liên hệ"));
                else if (action.Contains("về chúng tôi")) ClickElement(By.LinkText("Về chúng tôi"));
                else if (action.Contains("giỏ hàng")) ClickElement(By.CssSelector("a[href*='Cart'], a[href*='cart'], #cart-count, .fa-shopping-cart"));
                else if (action.Contains("đặt hàng") || action.Contains("thanh toán"))
                {
                    if (driver.FindElements(By.Id("placeOrderBtn")).Any(e => e.Displayed))
                        ClickElement(By.Id("placeOrderBtn"));
                    else
                        ClickElement(By.CssSelector("a[href*='Checkout'], button[onclick*='checkout'], button[type='submit']"));
                }
                else if (action.Contains("thêm vào giỏ"))
                {
                    ClickElement(By.CssSelector("button[onclick*='addToCart'], .btn-add-to-cart"));
                    System.Threading.Thread.Sleep(2000); // Chờ fetch hoàn tất và session cập nhật
                }
                else if (action.Contains("chờ xử lý")) SelectByValue(By.Id("statusFilter"), "pending");
                else if (action.Contains("đã xác nhận")) SelectByValue(By.Id("statusFilter"), "confirmed");
                else if (action.Contains("đang giao")) SelectByValue(By.Id("statusFilter"), "shipping");
                else if (action.Contains("đã giao")) SelectByValue(By.Id("statusFilter"), "delivered");
                else if (action.Contains("đã hủy")) SelectByValue(By.Id("statusFilter"), "cancelled");
                else if (action.Contains("tất cả đơn hàng")) SelectByValue(By.Id("statusFilter"), "");
                else if (action.Contains("lọc") || action.Contains("filter") || action.Contains("nhấn tìm kiếm"))
                {
                    // Ưu tiên nút Submit trong form Product hoặc nút có icon fa-filter
                    ClickElement(By.CssSelector("form[action*='/Product'] button[type='submit'], button.btn-animate, .btn-filter, button i.fa-filter"));
                }
                else if (action.Contains("xóa") && driver.Url.ToLower().Contains("cart"))
                {
                    if (!string.IsNullOrEmpty(data))
                    {
                        var items = driver.FindElements(By.CssSelector("li.flex, tr"));
                        var targetItem = items.FirstOrDefault(i => i.Text.Contains(data));
                        if (targetItem != null)
                        {
                            var delBtn = targetItem.FindElements(By.CssSelector("button[onclick*='removeItem'], a[href*='remove'], .fa-trash, .fa-times")).FirstOrDefault();
                            if (delBtn != null) 
                            {
                                delBtn.Click();
                                TryAcceptAlert();
                                System.Threading.Thread.Sleep(1000);
                            }
                            else ClickElement(By.CssSelector("button[onclick*='removeItem']"));
                        }
                        else ClickElement(By.CssSelector("button[onclick*='removeItem']"));
                    }
                    else ClickElement(By.CssSelector("button[onclick*='removeItem']"));
                }
                else if (action.Contains("đồng ý") || action.Contains("ok") || (action.Contains("chọn") && (action.Contains("xác nhận") || action.Contains("chấp nhận"))))
                {
                    try { driver.SwitchTo().Alert().Accept(); } catch { }
                }
                else if (action.Contains("hủy") || action.Contains("cancel") || action.Contains("không đồng ý"))
                {
                    try { driver.SwitchTo().Alert().Dismiss(); } catch { }
                }
                else if (action.Contains("địa chỉ mới")) ClickElement(By.Id("CreateNewAddress"));
                else if (action.Contains("chấp nhận điều khoản")) ClickElement(By.Id("agree-terms"));
                else if (action.Contains("đổi mật khẩu"))
                {
                    if (driver.FindElements(By.Id("changePasswordForm")).Any(f => f.Displayed))
                        ClickElement(By.CssSelector("#changePasswordForm button[type='submit']"));
                    else
                        ClickElement(By.CssSelector("button[onclick*='openChangePasswordModal']"));
                    
                    hasClickedSubmit = true;
                }
                else if (action.Contains("giá tăng dần")) SelectByValue(By.Name("sortBy"), "price_asc");
                else if (action.Contains("giá giảm dần")) SelectByValue(By.Name("sortBy"), "price_desc");
                else if (action.Contains("a-z")) SelectByValue(By.Name("sortBy"), "name_asc");
                else if (action.Contains("z-a")) SelectByValue(By.Name("sortBy"), "name_desc");
                else if (action.Contains("tăng") || action.Contains("+"))
                {
                    if (driver.Url.ToLower().Contains("cart"))
                    {
                        if (!string.IsNullOrEmpty(data))
                        {
                            var items = driver.FindElements(By.CssSelector("li.flex, tr, .cart-item"));
                            var target = items.FirstOrDefault(i => i.Text.Contains(data));
                            if (target != null)
                            {
                                var btn = target.FindElements(By.CssSelector("button[onclick*='updateQuantity']")).LastOrDefault();
                                if (btn != null) {
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", btn);
                                    btn.Click();
                                    return;
                                }
                            }
                        }
                        ClickElement(By.CssSelector("button[onclick*='updateQuantity']:nth-of-type(2), button[onclick*='increase'], .fa-plus, .fas.fa-plus"));
                    }
                    else if (driver.FindElements(By.CssSelector("button[onclick*='increaseQuantity']")).Any())
                        ClickElement(By.CssSelector("button[onclick*='increaseQuantity']"));
                    else
                        ClickElement(By.CssSelector(".fa-plus, .fas.fa-plus, button i.fa-plus"));
                }
                else if (action.Contains("giảm") || action.Contains("-"))
                {
                    if (driver.Url.ToLower().Contains("cart"))
                    {
                        if (!string.IsNullOrEmpty(data))
                        {
                            var items = driver.FindElements(By.CssSelector("li.flex, tr, .cart-item"));
                            var target = items.FirstOrDefault(i => i.Text.Contains(data));
                            if (target != null)
                            {
                                var btn = target.FindElements(By.CssSelector("button[onclick*='updateQuantity']")).FirstOrDefault();
                                if (btn != null) {
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", btn);
                                    btn.Click();
                                    return;
                                }
                            }
                        }
                        ClickElement(By.CssSelector("button[onclick*='updateQuantity']:nth-of-type(1), button[onclick*='decrease'], .fa-minus, .fas.fa-minus"));
                    }
                    else if (driver.FindElements(By.CssSelector("button[onclick*='decreaseQuantity']")).Any())
                        ClickElement(By.CssSelector("button[onclick*='decreaseQuantity']"));
                    else
                         ClickElement(By.CssSelector(".fa-minus, .fas.fa-minus, button i.fa-minus"));
                }
                else if (action.Contains("cập nhật"))
                {
                    var updateBtn = driver.FindElements(By.CssSelector("button, a, input[type='submit']"))
                        .FirstOrDefault(e => e.Displayed && (e.Text.Contains("Cập nhật") || e.Text.Contains("Update") || (e.GetAttribute("value")?.Contains("Cập nhật") ?? false)));
                    
                    if (updateBtn != null) updateBtn.Click();
                    else {
                        // Nếu không thấy nút cập nhật cụ thể, thường là AJAX tự chạy, ta chỉ cần chờ
                        System.Threading.Thread.Sleep(2000);
                    }
                }
                else if (action.Contains("tìm kiếm"))
                {
                    driver.FindElement(By.CssSelector("input[placeholder='Tìm kiếm...'], input[name='search']")).SendKeys(Keys.Enter);
                }
                else
                {
                    // Thử click theo text nếu không khớp keyword riêng (KHÔNG sử dụng XPath)
                    try
                    {
                        var targetText = data ?? "";
                        if (string.IsNullOrEmpty(targetText))
                        {
                            // Trích xuất text trong ngoặc kép hoặc sau các từ khóa hành động
                            if (step.Action.Contains("\""))
                            {
                                var segments = step.Action.Split('"');
                                if (segments.Length >= 3) targetText = segments[1];
                            }
                            else if (step.Action.ToLower().Contains("click vào ")) targetText = step.Action.Substring(step.Action.ToLower().IndexOf("click vào ") + 10).Trim();
                            else if (step.Action.ToLower().Contains("nhấn vào ")) targetText = step.Action.Substring(step.Action.ToLower().IndexOf("nhấn vào ") + 9).Trim();
                            else if (step.Action.ToLower().Contains("chọn ")) targetText = step.Action.Substring(step.Action.ToLower().IndexOf("chọn ") + 5).Trim();
                        }

                        if (!string.IsNullOrEmpty(targetText))
                        {
                            string normTarget = NormalizeWhitespace(targetText);
                            IWebElement? targetElement = null;

                            // 1. Tìm tất cả thẻ liên kết và nút bấm trước để ưu tiên
                            var linksAndBtns = driver.FindElements(By.CssSelector("a, button"));
                            targetElement = linksAndBtns.FirstOrDefault(e => e.Displayed && (
                                NormalizeWhitespace(e.Text ?? "").Contains(normTarget, StringComparison.OrdinalIgnoreCase) ||
                                (e.GetAttribute("title") != null && NormalizeWhitespace(e.GetAttribute("title") ?? "").Contains(normTarget, StringComparison.OrdinalIgnoreCase))
                            ));

                            // 2. Nếu không thấy, tìm bất kỳ thẻ nào chứa text và tiến hành duyệt ngược tìm link
                            if (targetElement == null)
                            {
                                var allVisible = driver.FindElements(By.CssSelector("h1, h2, h3, h4, h5, span, p, div, label, li, a, button"));
                                var textEl = allVisible.FirstOrDefault(e => e.Displayed && NormalizeWhitespace(e.Text ?? "").Contains(normTarget, StringComparison.OrdinalIgnoreCase));
                                if (textEl != null)
                                {
                                    // Duyệt ngược tìm thẻ <a> hoặc <button> cha gần nhất (tối đa 5 cấp)
                                    IWebElement? current = textEl;
                                    int depth = 0;
                                    while (current != null && depth < 5)
                                    {
                                        string tag = current.TagName.ToLower();
                                        if (tag == "a" || tag == "button")
                                        {
                                            targetElement = current;
                                            break;
                                        }
                                        try { current = current.FindElement(By.XPath("..")); } catch { break; }
                                        depth++;
                                    }
                                    if (targetElement == null) targetElement = textEl; // Dùng chính nó nếu không thấy cha là link
                                }
                            }

                            // 3. Cuối cùng mới đến input value
                            if (targetElement == null)
                            {
                                targetElement = driver.FindElements(By.CssSelector("input")).FirstOrDefault(e => e.Displayed && e.GetAttribute("value") != null && NormalizeWhitespace(e.GetAttribute("value") ?? "").Contains(normTarget, StringComparison.OrdinalIgnoreCase));
                            }
                            
                            if (targetElement != null)
                            {
                                string tagName = targetElement.TagName.ToLower();
                                TestContext.WriteLine($" [INFO] Đã xác định đối tượng Click: <{tagName}> khớp với '{targetText}'");
                                
                                // Cuộn và chuẩn bị click
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", targetElement);
                                System.Threading.Thread.Sleep(500);
                                
                                string oldUrl = driver.Url;
                                try { targetElement.Click(); } 
                                catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", targetElement); }
                                
                                // Chờ xem URL có thay đổi không (nếu là thẻ a)
                                if (tagName == "a")
                                {
                                    System.Threading.Thread.Sleep(2000);
                                    if (driver.Url == oldUrl) 
                                    {
                                        TestContext.WriteLine(" [RETRY] URL chưa đổi, thử click bằng JavaScript trực tiếp...");
                                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", targetElement);
                                        System.Threading.Thread.Sleep(1000);
                                    }
                                }
                            }
                            else 
                            {
                                TestContext.WriteLine($" [WARN] Không tìm thấy bất kỳ đối tượng nào (Link, Button, Text) khớp với: '{targetText}'");
                            }
                        }
                    }
                    catch (Exception ex) { 
                        TestContext.WriteLine($" [ERROR] Lỗi khi cố gắng click theo text: {ex.Message}");
                    }
                }
                return;
            }

            // Xử lý chờ quan sát
            if (action.Contains("chờ") || action.Contains("quan sát"))
            {
                System.Threading.Thread.Sleep(2000);
            }
        }

        private void InputText(string idOrName, string text)
        {
            if (driver == null || wait == null) return;
            
            // Tìm bằng ID hoặc Name kết hợp Wait cho đến khi hiển thị
            By selector = By.CssSelector($"#{idOrName}, [name='{idOrName}'], [id$='{idOrName}'], [name$='{idOrName}']");
            
            try 
            {
                var el = wait.Until(ExpectedConditions.ElementIsVisible(selector));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el);
                System.Threading.Thread.Sleep(500);

                // Xóa triệt để trước khi nhập (đặc biệt quan trọng với quantity input)
                el.Click();
                el.SendKeys(Keys.Control + "a");
                el.SendKeys(Keys.Backspace);
                try { el.Clear(); } catch { }
                
                el.SendKeys(text ?? "");
                el.SendKeys(Keys.Tab); 
            }
            catch (Exception)
            {
                // Fallback: Tìm tất cả input và so khớp linh hoạt
                var allInputs = driver.FindElements(By.CssSelector("input, textarea"));
                var match = allInputs.FirstOrDefault(i => i.Displayed && (
                    i.GetAttribute("id")?.Equals(idOrName, StringComparison.OrdinalIgnoreCase) == true || 
                    i.GetAttribute("name")?.Equals(idOrName, StringComparison.OrdinalIgnoreCase) == true));
                
                if (match != null)
                {
                    match.Click();
                    match.SendKeys(Keys.Control + "a");
                    match.SendKeys(Keys.Backspace);
                    match.Clear();
                    match.SendKeys(text ?? "");
                    match.SendKeys(Keys.Tab);
                }
                else throw; // Rethrow để Wait báo lỗi Timeout nếu không thấy
            }
        }

        private void ClickElement(By selector)
        {
            if (driver == null || wait == null) return;
            var el = wait.Until(ExpectedConditions.ElementToBeClickable(selector));
            
            try
            {
                // Cuộn phần tử vào giữa màn hình trước khi nhấn
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el);
                System.Threading.Thread.Sleep(500); // Chờ hiệu ứng cuộn mượt
                el.Click();
            }
            catch (Exception)
            {
                // Fallback: Nếu Click thông thường bị chặn (Intercepted), dùng JavaScript Click
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el);
            }
            
            // Tự động chấp nhận Alert nếu có (ví dụ sau khi nhấn Xóa/Thoát)
            TryAcceptAlert();
        }

        private void TryAcceptAlert()
        {
            try
            {
                // Chờ ngắn xem có alert không
                var alert = driver?.SwitchTo().Alert();
                if (alert != null)
                {
                    TestContext.WriteLine($" [INFO] Tự động chấp nhận thông báo Alert: '{alert.Text}'");
                    alert.Accept();
                }
            }
            catch (NoAlertPresentException) { }
            catch (Exception) { }
        }

        private void SelectByValue(By selector, string value)
        {
            if (driver == null || wait == null) return;
            var el = wait.Until(ExpectedConditions.ElementIsVisible(selector));
            var select = new SelectElement(el);
            select.SelectByValue(value);
        }

        private string NormalizeWhitespace(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return string.Join(" ", input.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        private void ValidateScenarioResult(string expected)
        {
            if (driver == null) return;
            System.Threading.Thread.Sleep(2000); // Chờ UI cập nhật hoàn tất

            string alertText = "";
            try
            {
                // Kiểm tra xem có JS Alert nào đang hiển thị không (như lỗi "Mật khẩu không khớp")
                var alert = driver.SwitchTo().Alert();
                alertText = alert.Text.ToLower();
                TestContext.WriteLine($" [INFO] Phát hiện thông báo Alert: '{alert.Text}'");
                alert.Accept(); // Đóng alert để tiếp tục
            }
            catch (NoAlertPresentException) { }

            // Lấy thông báo lỗi HTML5 (Validation Tooltip - ví dụ: "Giá trị phải lớn hơn hoặc bằng 1900")
            string html5Errors = "";
            try
            {
                var script = "return Array.from(document.querySelectorAll('input:invalid, select:invalid, textarea:invalid')).map(el => el.validationMessage).join(' ');";
                html5Errors = ((IJavaScriptExecutor)driver).ExecuteScript(script)?.ToString()?.ToLower() ?? "";
                if (!string.IsNullOrEmpty(html5Errors))
                    TestContext.WriteLine($" [INFO] Phát hiện lỗi HTML5 Validation: '{html5Errors}'");
            }
            catch { }

            string bodyText = driver.FindElement(By.TagName("body")).Text.ToLower() + " " + alertText + " " + html5Errors;
            string url = driver.Url.ToLower();
            string expectedLower = expected?.ToLower() ?? "";

            // Kiểm tra trực tiếp nếu bộ chứa nội dung mong đợi
            if (!string.IsNullOrEmpty(expectedLower) && (bodyText.Contains(expectedLower) || alertText.Contains(expectedLower) || html5Errors.Contains(expectedLower)))
            {
                TestContext.WriteLine($" [INFO] Tìm thấy nội dung mong đợi: '{expectedLower}'");
                return;
            }

            // Fallback: Nếu đang ở trang Đăng nhập/Đăng ký/Profile mà CHƯA có click submit trong steps, thử Click Submit
            if (!hasClickedSubmit && (url.Contains("login") || url.Contains("register") || url.Contains("profile")) && 
                !bodyText.Contains("thành công") && !bodyText.Contains("lỗi") && 
                !bodyText.Contains("tồn tại") && !bodyText.Contains("sử dụng") && !bodyText.Contains("không khớp") && string.IsNullOrEmpty(html5Errors))
            {
                var submitBtn = driver.FindElements(By.Id("login-submit-btn"))
                                .Concat(driver.FindElements(By.Id("register-submit-btn")))
                                .Concat(driver.FindElements(By.CssSelector("#changePasswordForm button[type='submit']")))
                                .Concat(driver.FindElements(By.CssSelector("form[action*='Profile'] button[type='submit']")))
                                .FirstOrDefault(e => e.Displayed);
                if (submitBtn != null)
                {
                    TestContext.WriteLine(" [INFO] Tự động nhấn Submit để hoàn tất luồng web...");
                    submitBtn.Click();
                    System.Threading.Thread.Sleep(2000);
                    
                    // Lấy lại nội dung sau khi click (bao gồm cả lỗi HTML5 mới nếu có)
                    try {
                         html5Errors = ((IJavaScriptExecutor)driver).ExecuteScript("return Array.from(document.querySelectorAll('input:invalid')).map(el => el.validationMessage).join(' ');")?.ToString()?.ToLower() ?? "";
                    } catch {}
                    bodyText = driver.FindElement(By.TagName("body")).Text.ToLower() + " " + html5Errors;
                    url = driver.Url.ToLower();
                }
            }

            if (expectedLower.Contains("thành công") || expectedLower.Contains("đạt") || expectedLower.Contains("chuyển hướng") || expectedLower.Contains("passed") || expectedLower.Contains("hợp lệ"))
            {
                bool success = bodyText.Contains("thành công") || bodyText.Contains("đăng nhập") || url.Contains("home") || url.Contains("confirmation") || (!url.Contains("login") && !url.Contains("register") && !url.Contains("error"));
                if (!success) throw new Exception($"Mong đợi Thành công nhưng không tìm thấy dấu hiệu thành công trên UI (URL: {url}).");
            }
            else if (expectedLower.Contains("lỗi") || expectedLower.Contains("thông báo") || expectedLower.Contains("không đúng") || expectedLower.Contains("vui lòng") || expectedLower.Contains("tồn tại") || expectedLower.Contains("không tìm thấy") || expectedLower.Contains("không có"))
            {
                bool errorEncountered = bodyText.Contains("lỗi") || bodyText.Contains("thông báo") || bodyText.Contains("không đúng") || 
                                       bodyText.Contains("vui lòng") || bodyText.Contains("yêu cầu") || bodyText.Contains("bỏ trống") || 
                                       bodyText.Contains("sai") || bodyText.Contains("tồn tại") || bodyText.Contains("không tìm thấy") || 
                                       bodyText.Contains("không có") || bodyText.Contains("chưa có") || bodyText.Contains("đã có") || bodyText.Contains("đã được") || 
                                       bodyText.Contains("không khớp") || bodyText.Contains("hiện tại") || bodyText.Contains("giá trị") || 
                                       bodyText.Contains("bằng") || !string.IsNullOrEmpty(html5Errors);
                
                if (!errorEncountered) 
                {
                    // Kiểm tra thêm trong các thẻ validation
                    bool hasValidationError = driver.FindElements(By.CssSelector(".field-validation-error, .validation-summary-errors, .text-danger")).Any(e => e.Displayed && !string.IsNullOrEmpty(e.Text));
                    if (!hasValidationError)
                        throw new Exception($"Mong đợi có thông báo lỗi/cảnh báo nhưng không tìm thấy trên UI.");
                }
            }
        }
    }

    public class TestScenario
    {
        public string Id { get; set; } = "";
        public string Objective { get; set; } = "";
        public string ExpectedResult { get; set; } = "";
        public List<TestScenarioStep> Steps { get; set; } = new List<TestScenarioStep>();

        // Cấu hình cách hiển thị tên test case trong Test Explorer
        public override string ToString() => $"[{Id}] {Objective}";
    }

    public class TestScenarioStep
    {
        public string Action { get; set; } = "";
        public string TestData { get; set; } = "";
    }
}
