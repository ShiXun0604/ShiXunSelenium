using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiXunSeleniumTools
{
    public enum ErrorType
    {
        ScriptActionNotFound,
        InvalidActionValue,
        InvalidConditionValue,
        InvalidMethodValue,
        InvalidSelectValue,
        InvalidDirectionValue,
        InvalidLogicDefinition,
        JsonFileNotFound,
        ElementWaitTimeout,
        ElementNotFound,
        ElementNotSelect,
        ElementNotCheckbox,
        ExceedMaximumExecuteStepCount,
        ElementNotSatifyCondition
    }
    public abstract class ShiXunSeleniumException : Exception
    {        
        public string errorPrompt;
        public abstract ErrorType ErrorType { get; }
        public ShiXunSeleniumException(string errorPrompt) : base(errorPrompt)
        {
            this.errorPrompt = errorPrompt;
        }
        public ShiXunSeleniumException(string errorPrompt, Exception innerException) : base(innerException.Message, innerException)
        {
            this.errorPrompt = errorPrompt;
        }
    }
    /// <summary>
    /// Json file中action欄位的值不正確時拋出
    /// </summary>
    public class ScriptActionNotFoundException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ScriptActionNotFound;
        public ScriptActionNotFoundException(string errorPrompt) : base(errorPrompt)
        {

        }
        public ScriptActionNotFoundException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {

        }
    }
    /// <summary>
    /// Json file中action欄位的值不正確時拋出
    /// </summary>
    public class InvalidActionValueException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.InvalidActionValue;
        public InvalidActionValueException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public InvalidActionValueException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// Json file中condition欄位的值不正確時拋出
    /// </summary>
    public class InvalidConditionValueException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.InvalidConditionValue;
        public InvalidConditionValueException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public InvalidConditionValueException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// Json file中method欄位的值不正確時拋出
    /// </summary>
    public class InvalidMethodValueException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.InvalidMethodValue;
        public InvalidMethodValueException(string errorPrompt) : base(errorPrompt)
        {
           
        }
        public InvalidMethodValueException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// Json file中select欄位的值不正確時拋出
    /// </summary>
    public class InvalidSelectValueException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.InvalidSelectValue;
        public InvalidSelectValueException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public InvalidSelectValueException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// Json file中direction欄位的值不正確時拋出
    /// </summary>
    public class InvalidDirectionValueException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.InvalidDirectionValue;
        public InvalidDirectionValueException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public InvalidDirectionValueException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// Json file中邏輯(if、while、for)定義不正確時拋出
    /// </summary>
    public class InvalidLogicDefinitionException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.InvalidLogicDefinition;
        public InvalidLogicDefinitionException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public InvalidLogicDefinitionException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
           
        }
    }

    /// <summary>
    /// 當Json file不存在時拋出
    /// </summary>
    public class JsonFileNotFoundException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.JsonFileNotFound;
        public JsonFileNotFoundException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public JsonFileNotFoundException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// 當執行WaitUntil時，超過設定的等待時間時拋出
    /// </summary>
    public class ElementWaitTimeoutException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ElementWaitTimeout;
        public ElementWaitTimeoutException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public ElementWaitTimeoutException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }

    /// <summary>
    /// 當找不到指定的元素時拋出
    /// </summary>
    public class ElementNotFoundException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ElementNotFound;
        public ElementNotFoundException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public ElementNotFoundException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// 當執行FindSelect時元素不是select時拋出
    /// </summary>
    public class ElementNotSelectException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ElementNotSelect;
        public ElementNotSelectException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public ElementNotSelectException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// 當執行FindCheckbox時元素不是FindCheckbox時拋出
    /// </summary>
    public class ElementNotCheckboxException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ElementNotCheckbox;
        public ElementNotCheckboxException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public ElementNotCheckboxException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }
    /// <summary>
    /// 執行腳本時超過設定的最大執行步驟數時拋出
    /// </summary>
    public class ExceedMaximumExecuteStepCountException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ExceedMaximumExecuteStepCount;
        public ExceedMaximumExecuteStepCountException(string errorPrompt) : base(errorPrompt)
        {
            
        }
        public ExceedMaximumExecuteStepCountException(string errorPrompt, Exception innerException) : base(errorPrompt, innerException)
        {
            
        }
    }

    public class ElementNotSatifyConditionException : ShiXunSeleniumException
    {
        public override ErrorType ErrorType => ErrorType.ElementNotSatifyCondition;
        public ElementNotSatifyConditionException(string errorPrompt) : base(errorPrompt)
        {

        }
    }
}
