using Newtonsoft.Json;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiXunSeleniumTools
{
    /// <summary>
    /// 這個是對外開放的interface,用來管理Selenium相關的操作
    /// </summary>
    public abstract class ShiXunSeleniumManager
    {
        public string JsonFilePath { get; set; } = "";  // Json檔案路徑
        internal Dictionary<string, string> variableDict { get; set; } = new Dictionary<string, string>();  // 用來儲存變數的字典
        public abstract Dictionary<ErrorType, int> errorCodeDict { get; set; }

        // Selenium物件
        private EdgeOptions options;
        public EdgeDriver driver;

        // Json讀進來的資料
        private List<StepCommand> StepsList;
        private SeleniumSetting seleniumSetting;

        // program counter
        internal int PC;

        // 最大執行指令數量
        private int stepCount;  // 計算目前執行的指令數量
        public int maxStepCount = 300;

        // 邏輯指令資訊
        internal List<LogicOps> logicOpsList;  // 在precheck通過時儲存所有邏輯指令的相應index
        internal Stack<LogicOps> logicOpsStack;  // 實際執行時的logic stack

        public ShiXunSeleniumManager() { }
        public void LoadJsonStepsFile(string jsonFilePath)
        {
            this.JsonFilePath = jsonFilePath;
            // 讀取全部字串
            string jsonContent = null;

            // 重寫FileNotFoundException錯誤訊息
            AOP.ExecuteWithRewriteException(() => {
                jsonContent = File.ReadAllText(this.JsonFilePath);
            },
            catchType: typeof(FileNotFoundException),
            throwType: typeof(JsonFileNotFoundException), errorPrompt: $"File \"{this.JsonFilePath}\" can not found, please check your settings on PVWA.");

            // 反序列化(By ChatGPT)
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StepCommandConverter() }
            };
            JsonModel jsonStepsObject = JsonConvert.DeserializeObject<JsonModel>(jsonContent, settings);


            // 設定senenium參數
            this.seleniumSetting = jsonStepsObject.Config;
            this.options = new EdgeOptions();
            this.options.AddArgument("--window-size=1920,1080");
            // 無頭模式
            if (this.seleniumSetting.isHeadless)  // 如果json檔案有設定headless則啟用            
                this.options.AddArgument("--headless=new");
            if (this.seleniumSetting.isInPrivate)  // 啟動無痕模式
                this.options.AddArgument("-inprivate");
            if (this.seleniumSetting.isAutomationHidden)  // 隱藏自動化痕跡
            {
                this.options.AddArgument("--disable-blink-features=AutomationControlled");
                this.options.AddExcludedArgument("enable-automation");
                this.options.AddAdditionalOption("useAutomationExtension", false);
            }
            if (this.seleniumSetting.userAgent != null)  // 設定userAgent
                this.options.AddArgument(this.seleniumSetting.userAgent);
        }
        private void StepLogicStatementCheck()
        {
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

            return;
        }
    
        
    
    }
}
