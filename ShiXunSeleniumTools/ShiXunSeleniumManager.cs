using Newtonsoft.Json;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ShiXunSeleniumTools
{
    /// <summary>
    /// 這個是對外開放的interface class,用來管理Selenium相關的操作
    /// </summary>
    public partial class ShiXunSeleniumManager
    {
        public string jsonFilePath { get; set; } = "";  // Json檔案路徑
        public bool isDebug { get; set; } = true;  // 是否開啟debug模式
        public Dictionary<string, string> variableDict { get; set; } = new Dictionary<string, string>();  // 用來儲存變數的字典
        public List<string> clipboardRecord { get; set; } = new List<string>();  // 用來儲存剪貼簿的歷史紀錄
        //public abstract Dictionary<ErrorType, int> errorCodeDict { get; set; }

        // Selenium物件
        private EdgeOptions options;
        public EdgeDriver driver;

        // Json讀進來的資料
        private JsonModel jsonModel;
        private SeleniumSetting seleniumSetting;
        private Dictionary<string, List<StepCommand>> actions;
        private List<StepCommand> StepsList;        

        // program counter
        internal int PC;

        // 最大執行指令數量
        private int stepCount;  // 計算目前執行的指令數量
        public int maxStepCount = 300;

        // 邏輯指令資訊
        internal List<LogicOps> logicOpsList;  // 在precheck通過時儲存所有邏輯指令的相應index
        internal Stack<LogicOps> logicOpsStack;  // 實際執行時的logic stack
        internal ManualResetEvent PauseEvent = new ManualResetEvent(true);

        // log related
        internal LogLevel logLevel = LogLevel.OFF;
        internal string logFilePath = null;

        public ShiXunSeleniumManager(string jsonFilePath) 
        {
            this.jsonFilePath = jsonFilePath;
            this.LoadJsonStepsFile(jsonFilePath);
        }
        public void PauseContinue()
        {
            this.PauseEvent.Set();
        }
        public void LoadJsonStepsFile(string jsonFilePath)
        {
            this.Log(LogLevel.DEBUG, $"Loadding json steps file. File path: {jsonFilePath}");

            this.jsonFilePath = jsonFilePath;
            // 讀取全部字串
            string jsonContent = null;

            // 重寫FileNotFoundException錯誤訊息
            AOP.ExecuteWithRewriteException(() => {
                jsonContent = File.ReadAllText(this.jsonFilePath);
            },
            catchType: typeof(FileNotFoundException),
            throwType: typeof(JsonFileNotFoundException), errorPrompt: $"File \"{this.jsonFilePath}\" can not found, please check your settings on PVWA.");

            // 反序列化(By ChatGPT)
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StepCommandConverter() }
            };
            this.jsonModel = JsonConvert.DeserializeObject<JsonModel>(jsonContent, settings);
            this.seleniumSetting = this.jsonModel.Config;
            this.actions = this.jsonModel.Actions;

            // 設定Selenium參數
            this.SeleniumConfigure();
            /*
            // 執行Json檔案的合法性檢查
            if (this.isDebug)
                this.StepLogicStatementCheck();
            */
        }
        private void SeleniumConfigure()
        {
            // 設定senenium參數
            this.options = new EdgeOptions();           

            // 無頭模式
            if (this.seleniumSetting.isHeadless)          
                this.options.AddArgument("--headless=new");
            // 設定無痕模式
            if (this.seleniumSetting.isInPrivate)  
                this.options.AddArgument("-inprivate");
            // 隱藏自動化痕跡
            if (this.seleniumSetting.isAutomationHidden)  
            {
                this.options.AddArgument("--disable-blink-features=AutomationControlled");
                this.options.AddExcludedArgument("enable-automation");
                this.options.AddAdditionalOption("useAutomationExtension", false);
            }
            // 設定userAgent
            if (!String.IsNullOrEmpty(this.seleniumSetting.userAgent))  
                this.options.AddArgument(this.seleniumSetting.userAgent);
            // 設定瀏覽器大小
            if (!String.IsNullOrEmpty(this.seleniumSetting.browserSize))  
                this.options.AddArgument(this.seleniumSetting.browserSize);
            else
                this.options.AddArgument("--start-maximized");
            // 設定使用者資料路徑
            if (!String.IsNullOrEmpty(this.seleniumSetting.userProfilePath))  
                this.options.AddArgument($"--user-data-dir={this.seleniumSetting.userProfilePath}");


            this.Log(LogLevel.INFO, "Succesfully load json steps file.");
        }
        private void StepLogicStatementCheck()
        {
            foreach (string key in this.actions.Keys)
            {
                this.StepsList = this.actions[key];
                this.OneStepLogicStatementCheck();
            }
            this.StepsList = null;
        }
        private void OneStepLogicStatementCheck()
        {
            this.Log(LogLevel.DEBUG, $"Start checking logic statements in steps. Will check action \"{this.actions}\"");

            // 執行邏輯判斷式(if、while、for)的合法性檢查演算法
            this.logicOpsStack = new Stack<LogicOps>();
            this.logicOpsList = new List<LogicOps>();
            int PC = 0;  // Program Counter
            while (PC < this.StepsList.Count)
            {
                //StepCommand currStep = this.StepsList[PC];
                switch (this.StepsList[PC].action)
                {
                    #region ifStatement
                    case "If":
                        this.logicOpsStack.Push(new IfStatement(PC));
                        break;
                    case "Else":
                        // 如果stack都空了但是碰到else則報錯
                        if (this.logicOpsStack.Count == 0)
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"Else\" without \"If\", please check your json file");

                        // 如果stack的頂部是if則把當前的PC寫入elseIndex
                        if (this.logicOpsStack.Peek() is IfStatement ifOps)
                            ifOps.elseIndex = PC;
                        else  // 如果stack的頂部不是if但碰到else則報錯
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"Else\" without \"If\", please check your json file");

                        break;
                    case "EndIf":
                        // 如果stack都空了但是碰到endif則報錯
                        if (this.logicOpsStack.Count == 0)
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"EndIf\" without \"If\", please check your json file");

                        // 如果stack的頂部是if則把當前的PC寫入endIndex
                        if (this.logicOpsStack.Peek() is IfStatement ifOpss)
                        {
                            ifOpss.endIndex = PC;
                            this.logicOpsList.Add(ifOpss);  // 把合法的ifOpss放進合法的list
                            this.logicOpsStack.Pop();
                        }
                        else  // 如果stack的頂部不是if但碰到endif則報錯
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"EndIf\" without \"If\", please check your json file");
                        break;
                    #endregion
                    #region forStatement
                    case "ForLoop":
                        this.logicOpsStack.Push(new ForLoopStatement(PC));
                        break;
                    case "EndForLoop":
                        // 如果stack都空了但是碰到endforloop則報錯
                        if (this.logicOpsStack.Count == 0)
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"EndForLoop\" without \"ForLoop\", please check your json file");

                        // 如果stack的頂部是forloop則把當前的PC寫入endIndex
                        if (this.logicOpsStack.Peek() is ForLoopStatement forOps)
                        {
                            forOps.endIndex = PC;
                            this.logicOpsList.Add(forOps);  // 把合法的forOps放進合法的list
                            this.logicOpsStack.Pop();
                        }
                        else  // 如果stack的頂部不是forloop但碰到endforloop則報錯
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"EndForLoop\" without \"ForLoop\", please check your json file");

                        break;
                    #endregion
                    #region whileStatement
                    case "WhileLoop":
                        this.logicOpsStack.Push(new WhileLoopStatement(PC));
                        break;
                    case "EndWhileLoop":
                        // 如果stack都空了但是碰到endwhileloop則報錯
                        if (this.logicOpsStack.Count == 0)
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"EndWhileLoop\" without \"WhileLoop\", please check your json file");

                        // 如果stack的頂部是whileloop則把當前的PC寫入endIndex
                        if (this.logicOpsStack.Peek() is WhileLoopStatement whileOps)
                        {
                            whileOps.endIndex = PC;
                            this.logicOpsList.Add(whileOps);  // 把合法的whileOps放進合法的list
                            this.logicOpsStack.Pop();
                        }
                        else  // 如果stack的頂部不是whileloop但碰到endwhileloop則報錯
                            throw new InvalidLogicDefinitionException($"In step {PC}, \"EndWhileLoop\" without \"WhileLoop\", please check your json file");

                        break;
                        #endregion
                }
                PC += 1;
            }
            // 跑完整個步驟後如果stack還有東西則報錯
            if (this.logicOpsStack.Count != 0)
                throw new InvalidActionValueException($"You are missing closing element in your json definition, please check your json file");
            this.Log(LogLevel.INFO, $"Logic statements check completed. Found {this.logicOpsList.Count} logic statements in steps.");
            return;
        }
        public void LoggerSet(LogLevel logLevel, string logFilePath = null)
        {
            this.logLevel = logLevel;
            this.logFilePath = logFilePath;
        }

        public async Task RunningAsync(string iniUrl, string executeActionName)
        { 
            await Task.Run(() => Running(iniUrl, executeActionName));
        }
        public void Running(string iniUrl, string executeActionName)
        {
            this.Log(LogLevel.INFO, $"Start running action: {executeActionName} with url: {iniUrl}");

            if (this.actions.TryGetValue(executeActionName, out var steps))
            {
                // 載入步驟
                this.StepsList = steps;
                // 檢查邏輯判斷式
                this.OneStepLogicStatementCheck();
            }
            else
            {
                string errorMsg = $"Action '{executeActionName}' not found in action list.";
                this.Log(LogLevel.CRITICAL, errorMsg);
                throw new ScriptActionNotFoundException(errorMsg);
            }       
                
            if (string.IsNullOrEmpty(this.jsonFilePath))
            {
                string errorMsg = "Json file path is not set, please call LoadJsonStepsFile() first.";
                this.Log(LogLevel.CRITICAL, errorMsg);
                throw new JsonFileNotFoundException(errorMsg);
            }
                
            // 隱藏命令提示字元視窗
            EdgeDriverService service = EdgeDriverService.CreateDefaultService();
            if(!this.isDebug)
                service.HideCommandPromptWindow = true;

            // 開啟瀏覽器,瀏覽指定URL
            this.driver = new EdgeDriver(service, this.options);            
            this.driver.Navigate().GoToUrl(iniUrl);

            // 一步一步執行動作
            this.PC = 0;
            this.stepCount = 0;
            while (this.PC < this.StepsList.Count)
            {
                this.StepsList[this.PC].Execute(this);                
                this.Log(LogLevel.DEBUG, $"Finish step{this.PC+1}, action: {this.StepsList[this.PC].action}");                

                // 如果執行的步驟數量超過最大值則報錯
                this.PC += 1;
                this.stepCount += 1;
                if (this.stepCount > maxStepCount)
                {
                    string errorMsg = $"The number of steps exceeds the maximum value of {this.maxStepCount}, please check your json file to avoid infinite loop";
                    this.Log(LogLevel.CRITICAL, errorMsg);
                    throw new ExceedMaximumExecuteStepCountException(errorMsg);
                }
            }

            this.Log(LogLevel.INFO, "Finish execute scripts. All steps completed.");

            this.driver.Quit();  // 關閉瀏覽器
            this.driver.Dispose();
        }
    }
}
