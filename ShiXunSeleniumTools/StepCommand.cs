using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShiXunSeleniumTools
{
    internal abstract class StepCommand
    {
        public int step { get; set; }
        public string action { get; set; }
        public string select { get; set; }              // 此欄位不一定每個action都有
        public string selectValue { get; set; } = "";   // 此欄位不一定每個action都有
        public abstract void Execute(ShiXunSeleniumManager manager);
        /// <summary>
        /// 替換變數的值
        /// </summary>
        /// <param name="data">變數字串</param>
        /// <param name="para">BaseAction</param>
        /// <returns>變數替換完成的字串</returns>
        internal string VariableReplace(string data, Dictionary<string, string> variableDict)
        {
            // 用regular expression替換變數
            data = Regex.Replace(data, @"\$\{(.*?)\}", match =>
            {
                string key = match.Groups[1].Value;
                if (variableDict.TryGetValue(key, out var value))
                    return value ?? "";
                else if (key == "clipboardContent")
                {
                    string text = null;
                    Thread staThread = new Thread(() =>
                    {
                        if (Clipboard.ContainsText())
                        {
                            text = Clipboard.GetText();
                        }
                    });
                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                    staThread.Join();
                    Console.WriteLine($"Clipboard content: {text}");
                    return text;
                }
                else
                    return match.Value;
            });
            return data;
        }
        /// <summary>
        /// 根據json的select欄位回傳By物件
        /// </summary>
        /// <param name="replacedSelectValue"></param>
        /// <returns></returns>
        /// <exception cref="InvalidSelectValueException"></exception>
        internal By SelectorSetting(string replacedSelectValue)
        {
            By byObj;
            switch (this.select)
            {
                case "ById":
                    byObj = By.Id(replacedSelectValue);
                    break;
                case "ByName":
                    byObj = By.Name(replacedSelectValue);
                    break;
                case "ByXPath":
                    byObj = By.XPath(replacedSelectValue);
                    break;
                case "ByPartialLinkText":
                    byObj = By.PartialLinkText(replacedSelectValue);
                    break;
                case "ByLinkText":
                    byObj = By.LinkText(replacedSelectValue);
                    break;
                case "ByClassName":
                    byObj = By.ClassName(replacedSelectValue);
                    break;
                case "ByCssSelector":
                    byObj = By.CssSelector(replacedSelectValue);
                    break;
                case "ByTagName":
                    byObj = By.TagName(replacedSelectValue);
                    break;
                default:
                    throw new InvalidSelectValueException($"In step{this.step}, select \"{this.select}\" is not defined.");
            }
            return byObj;
        }
        /// <summary>
        /// 選取元素(會執行變數替換),若元素為空則回傳null
        /// </summary>
        /// <param name="para"></param>
        /// <param name="driver"></param>
        /// <returns></returns>
        /// <exception cref="ElementNotFoundException"></exception>
        internal IWebElement SelectElement(ShiXunSeleniumManager manager)
        {
            // 替換變數
            string replacedSelectValue = this.VariableReplace(this.selectValue, manager.variableDict);

            // 選取元素
            By selector = this.SelectorSetting(replacedSelectValue);
            IWebElement element;
            try
            {
                element = manager.driver.FindElement(selector);
            }
            catch (NoSuchElementException)
            {
                element = null;
            }
            return element;
        }
        /// <summary>
        /// 判斷元素是否符合條件
        /// 我覺得在這個function裡面盡量避免拋出exception,只回傳true或false就好
        /// </summary>
        /// <param name="element"></param>
        /// <param name="condition"></param>
        /// <param name="conditionPara"></param>
        /// <param name="errorPrompt"></param>
        /// <returns></returns>
        internal bool IsConditionSatisfy(out string errorPrompt, IWebElement element, string condition, string conditionPara)
        {
            // 預設輸出為false
            bool result = false;
            errorPrompt = "";

            // 先處理檢查條件是IsElementExist和IsElementNotExist和不需要元素的情況
            bool flag = true;
            switch (condition)
            {
                case "IsElementExist":
                    if (element != null)
                        result = true;
                    else
                        errorPrompt = "Element does not exist";
                    break;
                case "IsElementNotExist":
                    if (element == null)
                        result = true;
                    else
                        errorPrompt = "Element exists";
                    break;
                case "ByBoolValue":
                    result = bool.Parse(conditionPara);
                    break;
                default:
                    flag = false;
                    break;
            }
            if (flag)  // 如果是執行上述檢查條件就回傳結果
                return result;

            // 接著處理其他檢查條件,接下來的條件都要有元素
            if (element == null)  // 如果元素為空則返回false
            {
                errorPrompt = "Element does not exist";
                return false;
            }

            // 開始檢查條件
            switch (condition)
            {
                case "IsTextAreaTextEqual":
                    if (element.GetAttribute("value") == conditionPara)
                        result = true;
                    else
                        errorPrompt = $"Element attribute is not equal to {conditionPara}";
                    break;
                case "IsTextAreaTextNotEqual":
                    if (element.GetAttribute("value") != conditionPara)
                        result = true;
                    else
                        errorPrompt = $"Element attribute is equal to {conditionPara}";
                    break;
                case "IsTextAreaTextContain":
                    if (element.GetAttribute("value").Contains(conditionPara))
                        result = true;
                    else
                        errorPrompt = $"Element attribute does not contain {conditionPara}";
                    break;
                case "IsTextAreaTextNotContain":
                    if (!element.GetAttribute("value").Contains(conditionPara))
                        result = true;
                    else
                        errorPrompt = $"Element attribute contains {conditionPara}";
                    break;
                case "IsTextEqual":
                    if (element.Text == conditionPara)
                        result = true;
                    else
                        errorPrompt = $"Element text is not equal to {conditionPara}";
                    break;
                case "IsTextNotEqual":
                    if (element.Text != conditionPara)
                        result = true;
                    else
                        errorPrompt = $"Element text is equal to {conditionPara}";
                    break;
                case "IsTextContain":
                    if (element.Text.Contains(conditionPara))
                        result = true;
                    else
                        errorPrompt = $"Element text does not contain {conditionPara}";
                    break;
                case "IsTextNotContain":
                    if (!element.Text.Contains(conditionPara))
                        result = true;
                    else
                        errorPrompt = $"Element text contains {conditionPara}";
                    break;
                case "IsSelected":
                    if (element.Selected)
                        result = true;
                    else
                        errorPrompt = "Element is not selected";
                    break;
                case "IsNotSelected":
                    if (!element.Selected)
                        result = true;
                    else
                        errorPrompt = "Element is selected";
                    break;
                default:
                    errorPrompt = $"condition \"{condition}\" is not defined.";
                    break;
            }
            return result;
        }
    }
}
