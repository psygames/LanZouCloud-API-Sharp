using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using LanZouAPI;

namespace Test
{
    [TestClass]
    public class LanZouApiTest
    {
        private async Task<LanZouCloud> EnsureLoginCloud()
        {
            var cloud = new LanZouCloud();
            cloud.set_log_level(LanZouCloud.LogLevel.Info);
            var code = await cloud.login_by_cookie("1104264", "VWAHNAJgAjoPPwdhWzVUB1MxDTxdDVA2UmkBZwI0BTdXYl9tVzANNQc9VDcMXwBvVWRSMQpkAGIDOAIzAzYKPVUwB2cCMgI3DzsHYls1VDlTMA09XTJQYlIzATcCNQU1V2FfZFdgDTEHPFRmDGMAU1U0UmgKZQBnAzACYwM1Cj1VZQc9AmM%3D");
            Assert.IsTrue(code == LanZouCode.SUCCESS);
            return cloud;
        }

        [TestMethod]
        public async Task Login()
        {
            await EnsureLoginCloud();
        }

        [TestMethod]
        public async Task GetFileList()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.get_file_list();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            Assert.IsTrue(fileList.files != null);
        }

        [TestMethod]
        public async Task GetFolderList()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.get_folder_list();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            Assert.IsTrue(fileList.folders != null);
        }


        [TestMethod]
        public async Task GetFileInfoById()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.get_file_list();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            var fileInfo = await cloud.get_file_info_by_id(fileList.files[0].id);
            Assert.IsTrue(fileInfo.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(fileInfo.durl));
        }

        [TestMethod]
        public async Task DownloadFileById()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.get_file_list();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isDownloadingOK = false;
            bool isFinishOK = false;

            var info = await cloud.down_file_by_id(fileList.files[0].id, "download", false, _progress =>
            {
                if (_progress.state == DownloadProgressInfo.State.Start)
                    isStartOK = true;
                if (_progress.state == DownloadProgressInfo.State.Ready)
                    isReadyOK = true;
                if (_progress.state == DownloadProgressInfo.State.Downloading)
                    isDownloadingOK = true;
                if (_progress.state == DownloadProgressInfo.State.Finish && _progress.current == _progress.total)
                {
                    isFinishOK = true;
                }
            });

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.share_url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.filename));
            Assert.IsTrue(File.Exists(info.file_path));

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isDownloadingOK);
            Assert.IsTrue(isFinishOK);
        }


        [TestMethod]
        public async Task UploadFile()
        {
            var cloud = await EnsureLoginCloud();

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isUploadingOK = false;
            bool isFinishOK = false;

            var info = await cloud.upload_file(@"download/WwiseLauncher.exe", -1, false, _progress =>
            {
                if (_progress.state == UploadProgressInfo.State.Start)
                    isStartOK = true;
                if (_progress.state == UploadProgressInfo.State.Ready)
                    isReadyOK = true;
                if (_progress.state == UploadProgressInfo.State.Uploading)
                    isUploadingOK = true;
                if (_progress.state == UploadProgressInfo.State.Finish && _progress.current == _progress.total)
                {
                    isFinishOK = true;
                }
            });

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.share_url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.filename));
            Assert.IsTrue(info.file_id != 0);

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isUploadingOK);
            Assert.IsTrue(isFinishOK);
        }

    }
}
