using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShiXunSeleniumTools
{
    internal class FindElement : StepCommand
    {
        public string method { get; set; }
        public string methodPara { get; set; } = "";
        public int retryCount { get; set; } = 0;  // 預設不重試
        public int retryInterval { get; set; } = 1;  // 預設重試間隔1秒
        /// <summary>
        /// 選定元素並進行動做
        /// </summary>
        /// <param name="para">把Action本身傳遞進來</param>
        /// <param name="driver"></param>
        /// <exception cref="InvalidSelectValueException">如果json定義錯誤的select欄位時拋出。</exception>
        /// <exception cref="InvalidMethodValueException">如果json定義錯誤的method欄位時拋出。</exception>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            // 選取元素
            IWebElement element = this.SelectElement(manager)
                 ?? throw new ElementNotFoundException($"In step{this.step}, element \"{this.selectValue}\" can not be found");

            // 執行動作
            switch (this.method)
            {
                case "Click":
                    element.Click();
                    break;
                case "ClickUntilSuccess":
                    int currCount = 0;
                    bool isSuccess = false;
                    while (currCount <= this.retryCount)
                    {
                        try
                        {
                            element.Click();
                            isSuccess = true;
                            break;  // 如果點擊成功則跳出迴圈
                        }
                        catch
                        {
                            // 如果發生錯誤則等待一段時間後重試
                            manager.Log(LogLevel.WARNING, $"In step{this.step}, element \"{this.selectValue}\" click failed, retrying... ({currCount + 1}/{this.retryCount})");
                            System.Threading.Thread.Sleep(this.retryInterval * 1000);
                            currCount++;                            
                        }
                    }
                    if (!isSuccess)
                        throw new ElementWaitTimeoutException($"In step{this.step}, element \"{this.selectValue}\" click failed after {this.retryCount} retries.");
                    else
                        break;
                case "SendKeys":
                    // 在該元素寫入字串
                    this.methodPara = this.VariableReplace(this.methodPara, manager);
                    element.SendKeys(this.methodPara);
                    break;
                default:
                    throw new InvalidMethodValueException($"In step{this.step}, method \"{this.method}\" is not defined");
                }

            return;
        }
    }
    internal class WaitUntil : StepCommand
    {
        public string condition { get; set; }
        public int waitTime { get; set; } = 0;  // 預設不等待

        /// <summary>
        /// 等待直到指定的元素出現or可以點擊or...。
        /// </summary>
        /// <param name="para">把Action本身傳遞進來</param>
        /// <param name="driver"></param>
        /// <exception cref="InvalidSelectValueException">如果json定義錯誤的select欄位時拋出。</exception>
        /// <exception cref="WebDriverTimeoutException">如果超過json設定的等待時間時拋出。</exception>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            if (this.condition == "BySecond")  // 先處理單純的等待時間
            {
                System.Threading.Thread.Sleep(this.waitTime * 1000);
                return;
            }
            else
            {
                // 設定 WebDriver 顯式等待時間
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(this.waitTime));

                // 設定條件
                string replacedSelectValue = this.VariableReplace(this.selectValue, manager);
                By selector = this.SelectorSetting(replacedSelectValue);

                // 重寫WebDriverTimeoutException錯誤訊息
                AOP.ExecuteWithRewriteException(() => 
                {
                    switch (this.condition)
                    {
                        case "IsElementExist":
                            wait.Until(dvr =>
                            {
                                var element = dvr.FindElement(selector);
                                return element.Displayed ? element : null;
                            });
                            break;
                        case "IsElementNotExist":
                            wait.Until(dvr =>
                            {
                                var elements = dvr.FindElements(selector);
                                return elements.Count == 0 || !elements.Any(e => e.Displayed) ? new object() : null;
                            });
                            break;
                        case "IsElementToBeClickable":
                            wait.Until(dvr =>
                            {
                                var element = dvr.FindElement(selector);
                                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                                return (
                                element.Displayed   // 檢查元素有顯示
                                && element.Enabled  // 檢查元素是啟用狀態
                                && (bool)js.ExecuteScript(@"
                                    var elem = arguments[0];
                                    var rect = elem.getBoundingClientRect();
                                    var x = rect.left + rect.width / 2;
                                    var y = rect.top + rect.height / 2;
                                    var elAtPoint = document.elementFromPoint(x, y);
                                    return elem === elAtPoint || elem.contains(elAtPoint);
                                ", element)         // 檢查元素在視窗中可點擊
                                ) ? element : null;
                            });
                            break;
                        default:
                            throw new InvalidConditionValueException($"In step{this.step}, condition \"{this.condition}\" is not defined.");
                    }
                },
                catchType: typeof(WebDriverTimeoutException),
                throwType: typeof(ElementWaitTimeoutException), errorPrompt: $"In step{this.step}, waiting for condition \"{this.condition}\" timed out after {this.waitTime} seconds.");

                return;
            }
        }
    }
    internal class SwitchToIframe : StepCommand
    {
        public string target { get; set; } = "";
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            switch (this.target)
            {
                case "Frame":  // 切換到指定的iframe
                    // 選取元素
                    IWebElement element = this.SelectElement(manager)
                        ?? throw new ElementNotFoundException($"In step{this.step}, element \"{this.selectValue}\" can not be found");

                    // 切換進去
                    driver.SwitchTo().Frame(element);
                    break;
                case "DefaultContent":  // 切換回主頁面
                    driver.SwitchTo().DefaultContent();
                    break;
                case "ParentFrame":  // 切換回上一層
                    driver.SwitchTo().ParentFrame();
                    break;
                default:
                    throw new InvalidSelectValueException($"In step{this.step}, select \"{this.target}\" is not defined.");
            }

            return;
        }
    }
    internal class IsElementSatisfyCondition : StepCommand
    {
        public string condition { get; set; }
        public string conditionPara { get; set; } = "";
        public string successMessage { get; set; } = "Default sucess msg.";
        public string failMessage { get; set; } = "Default fail msg.";
        /// <summary>
        /// 偵測元素是否符合條件
        /// </summary>
        /// <exception cref="ElementNotSatifyConditionException">如果元素不符合條件拋出此錯誤</exception>
        /// <exception cref="InvalidConditionValueException"></exception>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            // 替換變數的值
            string replacedConditionPara = this.VariableReplace(this.conditionPara, manager);
            this.successMessage = this.VariableReplace(this.successMessage, manager);
            this.failMessage = this.VariableReplace(this.failMessage, manager);

            // 選取元素
            IWebElement element = this.SelectElement(manager);

            // 檢查條件
            string errorPromp;
            bool isCondition = this.IsConditionSatisfy(out errorPromp, element, this.condition, replacedConditionPara);
            if (isCondition)
            {
                // what will success message do?
            }
            else
            {
                // 如果條件不滿足則拋出錯誤
                throw new ElementNotSatifyConditionException($"In step{this.step}, {this.failMessage}");
            }

            /*
            // 處理字串，替代變數的位置
            switch (this.condition)
            {
                case "TextEqual":
                    if (element.Text != replacedConditionPara)
                        throw new ElementNotSatifyConditionException($"In step{this.step}, {this.failMessage}", errorCode);
                    break;
                case "TextContain":
                    if (element == null)
                        throw new ElementNotSatifyConditionException($"In step{this.step}, {this.failMessage}", errorCode);
                    if (!element.Text.Contains(replacedConditionPara))
                        throw new ElementNotSatifyConditionException($"In step{this.step}, {this.failMessage}", errorCode);
                    break;
                case "IsElementExist":
                    if (element == null)
                        throw new ElementNotSatifyConditionException($"In step{this.step}, {this.failMessage}", errorCode);
                    break;
                default:
                    throw new InvalidConditionValueException($"In step{this.step}, condition \"{this.condition}\" is not defined.");
            }
            */
            return;
        }
    }
    internal class GoToUrl : StepCommand
    {
        public string url { get; set; }
        /// <summary>
        /// 直接跳轉到指定的網址
        /// </summary>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            if (string.IsNullOrEmpty(url))
                throw new Exception($"In step{this.step}, url \"{this.url}\" is not defined.");

            // 處理字串，替代變數的位置
            string replacedUrl = this.VariableReplace(this.url, manager);

            // 直接跳轉到指定的網址
            driver.Navigate().GoToUrl(replacedUrl);

            return;
        }
    }
    internal class FindSelect : StepCommand
    {
        public string method { get; set; }
        public string methodPara { get; set; } = "";
        /// <summary>
        /// 選擇下拉選單的某個元素
        /// </summary>
        /// <param name="para">把Action本身傳遞進來</param>
        /// <param name="driver"></param>
        /// <exception cref="InvalidSelectValueException">如果json定義錯誤的select欄位時拋出。</exception>
        /// <exception cref="InvalidMethodValueException">如果json定義錯誤的method欄位時拋出。</exception>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            // 選取元素
            IWebElement element = this.SelectElement(manager)
                 ?? throw new ElementNotFoundException($"In step{this.step}, element \"{this.selectValue}\" can not be found");

            SelectElement selectElement = null;
            // 執行動作 重寫UnexpectedTagNameException錯誤訊息
            AOP.ExecuteWithRewriteException(() => {
                selectElement = new SelectElement(element);  // 如果不是<select>元素會拋出UnexpectedTagNameException
            },
            catchType: typeof(UnexpectedTagNameException),
            throwType: typeof(ElementNotSelectException), errorPrompt: $"In step{this.step}, FindSelect method needs to select <select> element");

            // 替換變數的值
            this.methodPara = this.VariableReplace(this.methodPara, manager);
            switch (this.method)
            {
                case "SelectByText":
                    selectElement.SelectByText(this.methodPara);
                    break;
                case "SelectByValue":
                    selectElement.SelectByValue(this.methodPara);
                    break;
                case "SelectByIndex":
                    selectElement.SelectByIndex(int.Parse(this.methodPara));
                    break;
                default:
                    throw new InvalidMethodValueException($"In step{this.step}, method \"{this.method}\" is not defined");
            }

            return;
        }
    }
    internal class FindCheckbox : StepCommand
    {
        public string method { get; set; }

        /// <summary>
        /// 勾選checkbox
        /// </summary>
        /// <param name="para">把Action本身傳遞進來</param>
        /// <param name="driver"></param>
        /// <exception cref="InvalidSelectValueException">如果json定義錯誤的select欄位時拋出。</exception>
        /// <exception cref="InvalidMethodValueException">如果json定義錯誤的method欄位時拋出。</exception>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            // 選取元素
            IWebElement element = this.SelectElement(manager)
                 ?? throw new ElementNotFoundException($"In step{this.step}, element \"{this.selectValue}\" can not be found");

            switch (this.method)
            {
                case "Check":
                    if (!element.Selected)
                        element.Click();
                    break;
                case "Uncheck":
                    if (element.Selected)
                        element.Click();
                    break;
                default:
                    throw new InvalidMethodValueException($"In step{this.step}, method \"{this.method}\" is not defined");
            }

            return;
        }
    }
    internal class ScrollWindow : StepCommand
    {
        public string direction { get; set; }
        public int scrollValue { get; set; }
        public int waitTime { get; set; } = 0;  // 預設不等待
        /// <summary>
        /// 滾動視窗
        /// </summary>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            // 滾動視窗
            switch (this.direction)
            {
                case "Up":
                    driver.ExecuteScript($"window.scrollBy(0, -{this.scrollValue});");
                    break;
                case "Down":
                    driver.ExecuteScript($"window.scrollBy(0, {this.scrollValue});");
                    break;
                case "Left":
                    driver.ExecuteScript($"window.scrollBy(-{this.scrollValue}, 0);");
                    break;
                case "Right":
                    driver.ExecuteScript($"window.scrollBy({this.scrollValue}, 0);");
                    break;
                default:
                    throw new InvalidDirectionValueException($"In step{this.step}, direction \"{this.direction}\" is not defined.");
            }

            // 等待指定的時間
            if (this.waitTime > 0)
            {
                System.Threading.Thread.Sleep(this.waitTime * 1000);
            }

            return;
        }
    }
    internal class MoveToElement : StepCommand
    {
        public int waitTime { get; set; } = 0;  // 預設不等待
        /// <summary>
        /// 滾動視窗
        /// </summary>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            // 選取元素
            IWebElement element = this.SelectElement(manager)
                 ?? throw new ElementNotFoundException($"In step{this.step}, element \"{this.selectValue}\" can not be found");

            // 滾動視窗
            Actions action = new Actions(driver);
            action.MoveToElement(element).Perform();

            // 等待指定的時間
            if (this.waitTime > 0)
            {
                System.Threading.Thread.Sleep(this.waitTime * 1000);
            }

            return;
        }
    }
    internal class ScrollOverflowDiv : StepCommand
    {
        public string direction { get; set; }
        public int scrollValue { get; set; }
        public int waitTime { get; set; }
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            // 選取元素(必須要是有overflow的div)
            IWebElement element = this.SelectElement(manager)
                 ?? throw new ElementNotFoundException($"In step{this.step}, element \"{this.selectValue}\" can not be found");

            // 偵錯(檢查是否為overflow的div)
            // reserve

            // 讀取並建立指令
            string executeScript;
            switch (this.direction)
            {
                case "Up":
                    executeScript = $"arguments[0].scrollTop -= {this.scrollValue};";
                    break;
                case "Down":
                    executeScript = $"arguments[0].scrollTop += {this.scrollValue};";
                    break;
                case "Left":
                    executeScript = $"arguments[0].scrollLeft -= {this.scrollValue};";
                    break;
                case "Right":
                    executeScript = $"arguments[0].scrollLeft += {this.scrollValue};";
                    break;
                default:
                    throw new InvalidDirectionValueException($"In step{this.step}, direction \"{this.direction}\" is not defined.");
            }

            // 執行滾動            
            ((IJavaScriptExecutor)driver).ExecuteScript(executeScript, element);

            // 等待指定的時間
            if (this.waitTime > 0)
            {
                System.Threading.Thread.Sleep(this.waitTime * 1000);
            }

            return;
        }
    }
    internal class If : StepCommand
    {
        public string condition { get; set; }
        public string conditionPara { get; set; } = "";
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            // 選取元素
            IWebElement element = this.SelectElement(manager);

            // 檢查條件
            string errorPromp;
            string replacedConditionPara = this.VariableReplace(this.conditionPara, manager);
            bool isCondition = this.IsConditionSatisfy(out errorPromp, element, this.condition, replacedConditionPara);

            // 根據isCondition決定PC該去哪
            var match = manager.logicOpsList.FirstOrDefault(op => op.startIndex == manager.PC);
            if (match is IfStatement IfOps)
            {
                // 創立新的statement物件(一定要新創一個)放到stack中
                IfStatement newIfOps = new IfStatement(IfOps.startIndex)
                {
                    endIndex = IfOps.endIndex,
                    elseIndex = IfOps.elseIndex,
                };
                manager.logicOpsStack.Push(newIfOps);

                // 如果條件滿足直接執行下一行程式碼即可
                if (isCondition)
                    return;
                // 如果條件不滿足而且有指定else則跳到else的下一行
                else if (!isCondition && IfOps.elseIndex != null)
                    manager.PC = (int)IfOps.elseIndex;
                // 如果條件不滿足而且沒有指定else則跳到endif
                else if (!isCondition && IfOps.elseIndex == null)
                    manager.PC = IfOps.endIndex - 1;
            }
            else
                Console.WriteLine("Should not in this situation, why?");

            return;
        }
    }
    internal class Else : StepCommand
    {
        /// <summary>
        /// 如果碰到else代表if是true的時候會碰到,因此要跳到endif
        /// </summary>
        /// <param name="para"></param>
        /// <param name="driver"></param>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            var match = manager.logicOpsStack.Peek();
            if (match is IfStatement IfOps)
                manager.PC = IfOps.endIndex - 1;  // 跳到endif            
            else
                Console.WriteLine("Should not in this situation, why??");

            return;
        }
    }
    internal class EndIf : StepCommand
    {
        /// <summary>
        /// 碰到EndIf時把stack中的if拿掉
        /// </summary>
        /// <param name="para"></param>
        /// <param name="driver"></param>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            var match = manager.logicOpsStack.Peek();
            if (match is IfStatement)
                manager.logicOpsStack.Pop();  // 把if拿掉            
            else
                Console.WriteLine("Should not in this situation, why???");
        }
    }
    internal class ForLoop : StepCommand
    {
        public string method { get; set; }
        public string methodPara { get; set; }
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            var match = manager.logicOpsList.FirstOrDefault(op => op.startIndex == manager.PC);
            switch (this.method)
            {
                case "LoopByCount":
                    // 取得迴圈次數
                    int loopCount = int.Parse(this.methodPara);

                    // 將迴圈放到stack中
                    if (match is ForLoopStatement ForOps)
                    {
                        // 創立新的statement物件(一定要新創一個)放到stack中
                        ForLoopStatement newForOps = new ForLoopStatement(ForOps.startIndex)
                        {
                            endIndex = ForOps.endIndex,
                            loopMode = this.method,
                            iterTime = loopCount,
                        };  // note: precheck時只會把startIndex、endIndex寫好,其餘值不能照抄
                        manager.logicOpsStack.Push(newForOps);
                    }
                    else
                        Console.WriteLine("Should not in this situation, why????");

                    break;
                case "LoopByEach":
                    // reserve for future development
                    break;
                default:
                    throw new InvalidMethodValueException($"In step{this.step}, method \"{this.method}\" is not defined");
            }
            return;
        }
    }
    internal class EndForLoop : StepCommand
    {
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            var match = manager.logicOpsStack.Peek();
            if (match is ForLoopStatement ForOps)
            {
                // 迴圈數加一
                ForOps.currIterTime += 1;

                switch (ForOps.loopMode)
                {
                    case "LoopByCount":
                        // 如果迴圈數還未達標則跳到迴圈的起始位置的下一行
                        if (ForOps.currIterTime < ForOps.iterTime)
                            manager.PC = ForOps.startIndex;
                        // 如果迴圈數達標則把stack中的forLoop拿掉
                        else
                            manager.logicOpsStack.Pop();
                        break;
                    case "LoopByEach":
                        // reserve for future development
                        break;
                }  // note : 邏輯上不會需要寫default,一定都在case中
            }
            else
                Console.WriteLine("Should not in this situation, why?????");
            return;
        }
    }
    internal class WhileLoop : StepCommand
    {
        public string condition { get; set; }
        public string conditionPara { get; set; } = "";
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            Dictionary<string, string> para = manager.variableDict;

            // 選取元素
            IWebElement element = this.SelectElement(manager);

            // 檢查條件
            string errorPromp;
            string replacedConditionPara = this.VariableReplace(this.conditionPara, manager);
            bool isCondition = this.IsConditionSatisfy(out errorPromp, element, this.condition, replacedConditionPara);

            // 根據isCondition決定PC該去哪
            var match = manager.logicOpsList.FirstOrDefault(op => op.startIndex == manager.PC);
            if (match is WhileLoopStatement WhileOps)
            {
                // 判斷是否要push新的whileLoop
                // 如果stack的whileLoop和目前的whileLoop是同一個,則不需要push新的whileLoop
                bool isPushNewWhileOps = true;
                if (manager.logicOpsStack.Count > 0)
                {
                    var stackTop = manager.logicOpsStack.Peek();
                    if (stackTop is WhileLoopStatement stackTopWhileOps)
                        if (stackTopWhileOps.startIndex == WhileOps.startIndex)
                            isPushNewWhileOps = false;
                }

                // 創立新的statement物件(一定要新創一個)放到stack中
                if (isPushNewWhileOps)
                {
                    WhileLoopStatement newIfOps = new WhileLoopStatement(WhileOps.startIndex)
                    {
                        endIndex = WhileOps.endIndex,
                    };
                    manager.logicOpsStack.Push(newIfOps);
                }

                // 如果條件滿足直接執行下一行程式碼即可
                if (isCondition)
                    return;
                // 如果條件不滿足則跳到endwhileloop的下一行 並且把whileLoop從stack中pop掉
                else if (!isCondition)
                {
                    manager.logicOpsStack.Pop();
                    manager.PC = (int)WhileOps.endIndex;
                }
            }
            else
                Console.WriteLine("Should not in this situation, why?");

            return;
        }
    }
    internal class EndWhileLoop : StepCommand
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="para"></param>
        /// <param name="driver"></param>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            //EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;

            var match = manager.logicOpsStack.Peek();
            // 如果碰到EndWhile則直接跳回到WhileLoop那一行
            if (match is WhileLoopStatement WhileOps)
                manager.PC = WhileOps.startIndex - 1;
            else
                Console.WriteLine("Should not in this situation, why?????");
            return;
        }
    }
    internal class ProgrammingPause : StepCommand
    {
        public int waitTime { get; set; } = 10;  // 預設等待10秒鐘

        /// <summary>
        /// 使用程式控制是否暫停(用在windows Form button click之類的內部程式觸發)
        /// </summary>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            // 觸發PauseEvent,讓程式暫停
            manager.PauseEvent.Reset();

            // 等待PauseEvent被觸發
            bool isTimeout = manager.PauseEvent.WaitOne(this.waitTime * 1000);  

            if (isTimeout)
            {
                // 如果超過等待時間則拋出TimeoutException
                throw new ElementWaitTimeoutException($"In step{this.step}, waiting for pause timed out after {this.waitTime} seconds.");
            }
            else
                return;
        }
    }
    internal class AddNewTabPage : StepCommand
    {
        public string url { get; set; } = "about:blank";  // 預設開啟一個空白頁面
        public override void Execute(ShiXunSeleniumManager manager)
        {
            ((IJavaScriptExecutor)manager.driver).ExecuteScript($"window.open('{this.url}','_blank');");
        }
    }
    internal class SwitchToTabPage : StepCommand
    {
        public int index { get; set; }
        public override void Execute(ShiXunSeleniumManager manager)
        {
            // 切換到指定的tab頁
            var tabs = manager.driver.WindowHandles;
            manager.driver.SwitchTo().Window(tabs[this.index]);
        }
    }
    internal class CloseTabPage : StepCommand
    {
        public int index { get; set; }  // 預設關閉第一個tab頁
        public override void Execute(ShiXunSeleniumManager manager)
        {
            // 關閉指定的tab頁
            var tabs = manager.driver.WindowHandles;
            if (index < tabs.Count)
            {
                manager.driver.SwitchTo().Window(tabs[index]);
                manager.driver.Close();

                // 關閉後自動切回第一個還活著的 tab
                var remainingTabs = manager.driver.WindowHandles;
                if (remainingTabs.Count > 0)
                {
                    manager.driver.SwitchTo().Window(remainingTabs[0]);
                }
            }
            else
            {
                throw new ElementNotFoundException($"In step{this.step}, tab index {this.index} does not exist.");
            }
        }
    }
    internal class RaiseAlert : StepCommand
    {
        public string information { get; set; } = "";
        public int alertTime { get; set; } = 5;  // 預設等5秒
        public override void Execute(ShiXunSeleniumManager manager)
        {
            string replacedScript = this.VariableReplace(this.information, manager);

            // 顯示 alert（用戶需手動點擊確定）
            var driver = manager.driver;
            driver.ExecuteScript($"alert(\"{replacedScript}\");");

            // 等待 alert 消失後再繼續，例如用 WebDriverWait
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(this.alertTime));
            try
            {
                wait.Until(drv =>
                {
                    try
                    {
                        drv.SwitchTo().Alert();  // 如果 alert 還在，回傳 false
                        return false;
                    }
                    catch (NoAlertPresentException)
                    {
                        return true;  // alert 已經被使用者關閉
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                // 超過時間還沒關閉，就自己關掉
                try
                {
                    var alert = driver.SwitchTo().Alert();
                    alert.Accept();  // 幫使用者按下確定
                }
                catch
                {
                    // 已經被使用者關了，就不用處理
                }
            }
        }
    }
    #region FOR DEVELOP
    internal class TakeScreenshot : StepCommand
    {
        public string fileName { get; set; }
        /// <summary>
        /// 擷取螢幕截圖
        /// </summary>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict;
             
            // 擷取螢幕截圖
            Screenshot screenshot = driver.GetScreenshot();

            // 儲存到指定的檔案
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            screenshot.SaveAsFile(filePath);
            return;
        }
    }
    internal class FetchEntirePage : StepCommand
    {
        public string fileName { get; set; }

        /// <summary>
        /// 擷取整個網頁的HTML
        /// </summary>
        public override void Execute(ShiXunSeleniumManager manager)
        {
            EdgeDriver driver = manager.driver;
            //Dictionary<string, string> para = manager.variableDict; 

            // 擷取整個網頁的HTML
            string pageSource = driver.PageSource;

            // 儲存到指定的檔案
            System.IO.File.WriteAllText(this.fileName, pageSource);

            return;
        }
    }
    #endregion
}
