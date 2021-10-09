using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestLogin()
        {
            LanZouAPI.LanZouCloud client = new LanZouAPI.LanZouCloud();
            var result = client.login_by_cookie("1104264", "VWAHNAJgAjoPPwdhWzVUB1MxDTxdDVA2UmkBZwI0BTdXYl9tVzANNQc9VDcMXwBvVWRSMQpkAGIDOAIzAzYKPVUwB2cCMgI3DzsHYls1VDlTMA09XTJQYlIzATcCNQU1V2FfZFdgDTEHPFRmDGMAU1U0UmgKZQBnAzACYwM1Cj1VZQc9AmM%3D");
            Assert.IsTrue(result == LanZouAPI.LanZouCode.SUCCESS);
            var files = client.get_file_list(-1);
            Assert.IsTrue(result == LanZouAPI.LanZouCode.SUCCESS);
        }
    }
}
