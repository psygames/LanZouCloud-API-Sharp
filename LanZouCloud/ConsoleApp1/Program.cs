using LanZouAPI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Test Begin!");
            var task = Task.Run(TestAsync);
            while (!task.IsCompleted) Thread.Sleep(1);
            Console.WriteLine("Test Finish!");
        }

        private static async Task TestAsync()
        {
            try
            {
                var lzy = new LanZouCloud();

                // login
                var result = await lzy.login_by_cookie("1104264", "VWAHNAJgAjoPPwdhWzVUB1MxDTxdDVA2UmkBZwI0BTdXYl9tVzANNQc9VDcMXwBvVWRSMQpkAGIDOAIzAzYKPVUwB2cCMgI3DzsHYls1VDlTMA09XTJQYlIzATcCNQU1V2FfZFdgDTEHPFRmDGMAU1U0UmgKZQBnAzACYwM1Cj1VZQc9AmM%3D");
                Console.WriteLine(result);

                // list file
                var files = await lzy.get_file_list(-1);
                Console.WriteLine(files[0]);

                // list dir
                var dirs = await lzy.get_dir_list(-1);
                Console.WriteLine(dirs[0]);

                // file info
                var f_info = await lzy.get_file_info_by_id(files[0].id);
                Console.WriteLine(f_info);

                // download 
                var download = await lzy.down_file_by_id(files[0].id, "download", false, _info =>
                {
                    if (_info.state != DownloadInfo.State.Downloading)
                        Console.WriteLine(_info);
                });
                Console.WriteLine(download);

                // upload
                var upload = await lzy.upload_file(@"download/ToDesk_Lite.exe", -1, false, _info =>
                {
                    Console.WriteLine(_info);
                });
                Console.WriteLine(upload);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
