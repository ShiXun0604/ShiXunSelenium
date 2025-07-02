using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiXunSeleniumTools
{
    #region JsonRelated
    internal class JsonModel
    {
        public SeleniumSetting Config { get; set; }
        public List<StepCommand> Verify { get; set; }
        public List<StepCommand> Logon { get; set; }
        public List<StepCommand> Change { get; set; }
        public List<StepCommand> PreReconcile { get; set; }
        public List<StepCommand> Reconcile { get; set; }
    }
    public class SeleniumSetting
    {
        public bool isHeadless { get; set; }
        public bool isInPrivate { get; set; }
        public bool isAutomationHidden { get; set; }
        public string userAgent { get; set; }
        public string browserSize { get; set; }
    }
    #endregion JsonRelated
    public class LogicOps
    {
        public int startIndex { get; set; }
        public int endIndex { get; set; }
    }
    public class IfStatement : LogicOps
    {
        public int? elseIndex { get; set; }
        public IfStatement(int startIndex)
        {
            this.startIndex = startIndex;
        }
    }
    public class ForLoopStatement : LogicOps
    {
        public string loopMode { get; set; }
        public int iterTime { get; set; }
        public int currIterTime { get; set; }
        public ForLoopStatement(int startIndex)
        {
            this.startIndex = startIndex;
            this.iterTime = iterTime;
        }
    }
    /// <summary>
    /// while迴圈比較特別,
    /// </summary>
    public class WhileLoopStatement : LogicOps
    {

        public WhileLoopStatement(int startIndex)
        {
            this.startIndex = startIndex;
        }
    }
}
