using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using LanZouCloudAPI;
using System;

namespace Test
{
    [TestClass]
    public class CommonTest
    {
        [TestMethod]
        public async Task Login()
        {
            await AConfig.AsyncLoginCloud();
        }

        [TestMethod]
        public async Task Logout()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var result = await cloud.Logout();
            Assert.IsTrue(result.code == LanZouCode.SUCCESS);
        }

        [TestMethod]
        public async Task GetFileList()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            Assert.IsTrue(fileList.files != null);
        }

        [TestMethod]
        public async Task GetFolderList()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var fileList = await cloud.GetFolderList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);
            Assert.IsTrue(fileList.folders != null);
        }

        [TestMethod]
        public async Task GetFileInfoById()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            var fileInfo = await cloud.GetFileInfo(fileList.files[0].id);
            Assert.IsTrue(fileInfo.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(fileInfo.durl));
        }

        [TestMethod]
        public async Task GetFolderInfoById()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var folderList = await cloud.GetFolderList();
            Assert.IsTrue(folderList.code == LanZouCode.SUCCESS);

            Assert.IsTrue(folderList.folders.Count > 0);
            var folderInfo = await cloud.GetFolderInfo(folderList.folders[0].id, 1, 2);
            Assert.IsTrue(folderInfo.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(folderInfo.name));
        }

        [TestMethod]
        public async Task RenameFolder()
        {
            var cloud = await AConfig.AsyncLoginCloud();

            var folderId = await AConfig.GetTestFolder(cloud);

            var info = await cloud.RenameFolder(folderId, AConfig.TestFolder + "_Rename");
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);

            // revert
            info = await cloud.RenameFolder(folderId, AConfig.TestFolder);
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
        }


        [TestMethod]
        public async Task RenameFile()
        {
            var cloud = await AConfig.AsyncLoginCloud();

            var fileId = await AConfig.GetTestFile(cloud);

            var info = await cloud.RenameFile(fileId, AConfig.TestFolder + "_Rename");

            if (info.code == LanZouCode.FAILED && info.message == "此功能仅会员使用，请先开通会员")
            {
                return;
            }

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);

            // revert
            info = await cloud.RenameFolder(fileId, AConfig.TestFolder);
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
        }


        [TestMethod]
        public async Task MoveFolder()
        {
            var cloud = await AConfig.AsyncLoginCloud();

            var folderId = await AConfig.GetTestFolder(cloud);
            var subF = await AConfig.GetTestFolder(cloud, "SUBBBBBBBBBBBBBBB", folderId);

            // upload
            var create = await cloud.UploadFile(AConfig.TestFilePath, null, subF);
            Assert.IsTrue(create.code == LanZouCode.SUCCESS);

            // move
            var info = await cloud.MoveFolder(subF, -1);
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);

            // delete
            var del = await cloud.DeleteFolder(info.id);
            Assert.IsTrue(del.code == LanZouCode.SUCCESS);
        }


        // TODO: more unit tests
    }
}
