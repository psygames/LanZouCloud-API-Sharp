using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using LanZouCloudAPI;
using System;
using System.Threading;

namespace Test
{
    [TestClass]
    public class UploadTest
    {
        [TestMethod]
        public async Task UploadFile()
        {
            var cloud = await AConfig.AsyncLoginCloud();

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isUploadingOK = false;
            bool isFinishOK = false;

            var info = await cloud.UploadFile(AConfig.TestFilePath, null, -1, true,
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
        public async Task UploadFileBigAndCancel()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var cts = new CancellationTokenSource();
            new Task(async () =>
                {
                    await Task.Delay(4000);
                    cts.Cancel();
                }).Start();
            var info = await cloud.UploadFile(AConfig.TestFileBigPath, null, -1, false, null, cts.Token);
            Assert.IsTrue(info.code == LanZouCode.TASK_CANCELED);
        }

        [TestMethod]
        public async Task UploadFileBig()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var info = await cloud.UploadFile(AConfig.TestFileBigPath, null, -1, true);
            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
        }
    }
}
