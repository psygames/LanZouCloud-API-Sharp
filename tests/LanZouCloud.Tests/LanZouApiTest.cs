using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using LanZouCloudAPI;
using System;

namespace Test
{
    [TestClass]
    public class LanZouApiTest
    {
        const string TestFolder = "LanZouApiTestFolder";
        const string TestFile = "LanZouApiTestFile.txt";

        private LanZouCloud Cloud()
        {
            var cloud = new LanZouCloud();
            cloud.SetLogLevel(LanZouCloud.LogLevel.Info);
            return cloud;
        }

        private async Task<long> GetTestFolder(LanZouCloud cloud)
        {
            long folderId = 0;
            var fileList = await cloud.GetFolderList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            var folder = fileList.folders.Find(a => a.name == TestFolder);
            if (folder == null)
            {
                var create = await cloud.CreateFolder(TestFolder);
                Assert.IsTrue(create.code == LanZouCode.SUCCESS);
                folderId = create.id;
            }
            else
            {
                folderId = folder.id;
            }
            return folderId;
        }


        private async Task<long> GetTestFile(LanZouCloud cloud)
        {
            long fileId = 0;
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            var file = fileList.files.Find(a => a.name == TestFolder);
            if (file == null)
            {
                var create = await cloud.UploadFile(TestFile);
                Assert.IsTrue(create.code == LanZouCode.SUCCESS);
                fileId = create.id;
            }
            else
            {
                fileId = file.id;
            }
            return fileId;
        }

        private async Task<LanZouCloud> EnsureLoginCloud()
        {
            var cloud = new LanZouCloud();
            cloud.SetLogLevel(LanZouCloud.LogLevel.Info);
            string[] cookies = File.ReadAllText("cookie.txt").Split(',');
            var login = await cloud.Login(cookies[0], cookies[1]);
            Assert.IsTrue(login.code == LanZouCode.SUCCESS);
            return cloud;
        }

        [TestMethod]
        public async Task Login()
        {
            await EnsureLoginCloud();
        }


        [TestMethod]
        public async Task Logout()
        {
            var cloud = await EnsureLoginCloud();
            var result = await cloud.Logout();
            Assert.IsTrue(result.code == LanZouCode.SUCCESS);
        }

        [TestMethod]
        public async Task GetFileList()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            Assert.IsTrue(fileList.files != null);
        }

        [TestMethod]
        public async Task GetFolderList()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.GetFolderList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            Assert.IsTrue(fileList.folders != null);
        }

        [TestMethod]
        public async Task GetFileInfoById()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            var fileInfo = await cloud.GetFileInfo(fileList.files[0].id);
            Assert.IsTrue(fileInfo.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(fileInfo.durl));
        }

        [TestMethod]
        public async Task GetFolderInfoById()
        {
            var cloud = await EnsureLoginCloud();
            var folderList = await cloud.GetFolderList();
            Assert.IsTrue(folderList.code == LanZouCode.SUCCESS);

            Assert.IsTrue(folderList.folders.Count > 0);
            var folderInfo = await cloud.GetFolderInfo(folderList.folders[0].id, 1, 2);
            Assert.IsTrue(folderInfo.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(folderInfo.name));
        }

        [TestMethod]
        public async Task DownloadFileById()
        {
            var cloud = await EnsureLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isDownloadingOK = false;
            bool isFinishOK = false;

            var info = await cloud.DownloadFile(fileList.files[0].id, "download", true,
                new Progress<ProgressInfo>(_progress =>
            {
                if (_progress.state == ProgressState.Start)
                    isStartOK = true;
                if (_progress.state == ProgressState.Ready)
                    isReadyOK = true;
                if (_progress.state == ProgressState.Progressing)
                    isDownloadingOK = true;
                if (_progress.state == ProgressState.Finish)
                    isFinishOK = true;
            }));

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.fileName));
            Assert.IsTrue(File.Exists(info.filePath));

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isDownloadingOK);
            Assert.IsTrue(isFinishOK);
        }


        [TestMethod]
        public async Task DownloadFileByUrl()
        {
            var cloud = Cloud();

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isDownloadingOK = false;
            bool isFinishOK = false;
            var url = "https://wwa.lanzoui.com//i934vvi";
            var info = await cloud.DownloadFileByUrl(url, "download", "", true,
                new Progress<ProgressInfo>(_progress =>
                {
                    if (_progress.state == ProgressState.Start)
                        isStartOK = true;
                    if (_progress.state == ProgressState.Ready)
                        isReadyOK = true;
                    if (_progress.state == ProgressState.Progressing)
                        isDownloadingOK = true;
                    if (_progress.state == ProgressState.Finish)
                        isFinishOK = true;
                }));

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.fileName));
            Assert.IsTrue(File.Exists(info.filePath));

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

            var info = await cloud.UploadFile(@"download/气功.docx", -1, true,
                new Progress<ProgressInfo>(_progress =>
            {
                if (_progress.state == ProgressState.Start)
                    isStartOK = true;
                if (_progress.state == ProgressState.Ready)
                    isReadyOK = true;
                if (_progress.state == ProgressState.Progressing)
                    isUploadingOK = true;
                if (_progress.state == ProgressState.Finish)
                    isFinishOK = true;
            }));

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.fileName));
            Assert.IsTrue(info.id != 0);

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isUploadingOK);
            Assert.IsTrue(isFinishOK);
        }

        [TestMethod]
        public async Task RenameFolder()
        {
            var cloud = await EnsureLoginCloud();

            var folderId = await GetTestFolder(cloud);

            var info = await cloud.RenameFolder(folderId, TestFolder + "_Rename");
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);

            // revert
            info = await cloud.RenameFolder(folderId, TestFolder);
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
        }


        [TestMethod]
        public async Task RenameFile()
        {
            var cloud = await EnsureLoginCloud();

            var folderId = await GetTestFile(cloud);

            var info = await cloud.RenameFile(folderId, TestFolder + "_Rename");
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);

            // revert
            info = await cloud.RenameFolder(folderId, TestFolder);
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
        }

        // TODO: more unit tests
    }
}
