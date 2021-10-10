using LanZouAPI;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Begin!");

            var lzy = new LanZouCloud();

            // login
            var result = lzy.login_by_cookie("1104264", "VWAHNAJgAjoPPwdhWzVUB1MxDTxdDVA2UmkBZwI0BTdXYl9tVzANNQc9VDcMXwBvVWRSMQpkAGIDOAIzAzYKPVUwB2cCMgI3DzsHYls1VDlTMA09XTJQYlIzATcCNQU1V2FfZFdgDTEHPFRmDGMAU1U0UmgKZQBnAzACYwM1Cj1VZQc9AmM%3D");
            Console.WriteLine(result);

            // list file
            var files = lzy.get_file_list(-1);
            Console.WriteLine(files[0]);

            // list dir
            var dirs = lzy.get_dir_list(-1);
            Console.WriteLine(dirs[0]);

            // file info
            var f_info = lzy.get_file_info_by_id(files[0].id);
            Console.WriteLine(f_info);

            // download 
            var download = lzy.down_file_by_id(files[0].id, "download", false, _info =>
            {
                if (_info.state != DownloadInfo.State.Downloading)
                    Console.WriteLine(_info);
            });
            Console.WriteLine(download);

            // upload
            var upload = lzy.upload_file(@"download/ToDesk_Lite.exe", -1, false, _info =>
            {
                Console.WriteLine(_info);
            });
            Console.WriteLine(upload);

            Console.WriteLine("Test Finish!");
        }
    }
}
