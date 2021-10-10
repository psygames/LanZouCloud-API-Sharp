using LanZouAPI;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var lzy = new LanZouCloud();
            var result = lzy.login_by_cookie("1104264", "VWAHNAJgAjoPPwdhWzVUB1MxDTxdDVA2UmkBZwI0BTdXYl9tVzANNQc9VDcMXwBvVWRSMQpkAGIDOAIzAzYKPVUwB2cCMgI3DzsHYls1VDlTMA09XTJQYlIzATcCNQU1V2FfZFdgDTEHPFRmDGMAU1U0UmgKZQBnAzACYwM1Cj1VZQc9AmM%3D");
            Console.WriteLine(result);
            var files = lzy.get_file_list(-1);
            Console.WriteLine(files[0]);
            var dirs = lzy.get_dir_list(-1);
            Console.WriteLine(dirs[0]);
            var f_info = lzy.get_file_info_by_id(files[0].id);
            Console.WriteLine(f_info);
            var download = lzy.down_file_by_id(files[0].id, "download");
            Console.WriteLine(download);

            Console.WriteLine("Finish!");
        }
    }
}
