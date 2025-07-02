using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiXunSeleniumTools
{
    public class AOP
    {
        public static void ExecuteWithRewriteException(Action action, Type catchType, Type throwType, string errorPrompt = null)
        {
            if (!typeof(Exception).IsAssignableFrom(catchType))
                throw new ArgumentException("catchType 必須是 Exception 的子類別");
            if (!typeof(ShiXunSeleniumException).IsAssignableFrom(throwType))
                throw new ArgumentException("throwType 必須是 ShiXunSeleniumException 的子類別");

            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (catchType.IsAssignableFrom(ex.GetType()))
                {
                    // 建立新的例外物件
                    ShiXunSeleniumException rewritten;

                    if (ex.Message != null)
                    {
                        rewritten = (ShiXunSeleniumException)Activator.CreateInstance(throwType, ex.Message)
                            ?? throw new InvalidOperationException("無法建立例外實體");
                        rewritten.errorPrompt = errorPrompt;
                    }
                    else
                    {
                        rewritten = (ShiXunSeleniumException)Activator.CreateInstance(throwType, errorPrompt)
                            ?? throw new InvalidOperationException("無法建立例外實體");
                    }
                    throw rewritten;
                }

                // 如果不是 catchType，就維持原本的拋出
                throw;
            }
        }
    }

}
