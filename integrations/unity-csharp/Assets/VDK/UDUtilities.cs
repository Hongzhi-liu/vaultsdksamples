using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;
namespace Vault
{
    public static class UDUtilities
    {
        /*
         *Converts matrix from Unity's left handed transformation convention ( y'=Ay) to
         * left handed system (y'=yA)
         */
        public static double[] GetUDMatrix(Matrix4x4 unityMat)
        {
            double[] udMat =
            {
                unityMat.m00,
                unityMat.m10,
                unityMat.m20,
                unityMat.m30,

                unityMat.m01,
                unityMat.m11,
                unityMat.m21,
                unityMat.m31,

                unityMat.m02,
                unityMat.m12,
                unityMat.m22,
                unityMat.m32,

                unityMat.m03,
                unityMat.m13,
                unityMat.m23,
                unityMat.m33
            };

         return udMat;
        }

        /*
         * attempts to load and returns all loaded UDS models in the scene
         */
        public static vdkRenderInstance[] getUDSInstances()
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag("UDSModel");
            int count = 0;
            vdkRenderInstance[] modelArray = new vdkRenderInstance[objects.Length];
            for (int i = 0; i < objects.Length; ++i)
            {
                Component component = objects[i].GetComponent("UDSModel");
                UDSModel model = component as UDSModel;

                if (!model.isLoaded)
                    model.LoadModel();

                if (model.isLoaded)
                {
                    modelArray[count].pointCloud = model.udModel.pModel;
                    modelArray[count].worldMatrix = UDUtilities.GetUDMatrix(model.pivotTranslation * model.modelScale * objects[i].transform.localToWorldMatrix * model.pivotTranslation.inverse);
                    count++;
                }
            }
            return modelArray.Where(m => (m.pointCloud != System.IntPtr.Zero)).ToArray();
        }
    }

    /*
     *Class responsible for managing all threads related to VDK licensing
     */
    public class VDKSessionThreadManager {
        bool logLicenseInformation = false;
        Thread keepAliveThread;
        Thread licenseLogThread;
        List<Thread> activeThreads = new List<Thread>();
        public VDKSessionThreadManager() {
            keepAliveThread = new Thread(new ThreadStart(KeepAlive));
            keepAliveThread.Start();
            activeThreads.Add(keepAliveThread);
            if (logLicenseInformation)
            {
                licenseLogThread = new Thread(new ThreadStart(LogLicenseStatus));
                licenseLogThread.Start();
                activeThreads.Add(licenseLogThread);
            }
        }

        /*
         * Polls the license server once every 30 seconds to keep the session alive
         */
        public void KeepAlive() {
            while (true)
            {
                try
                {
                    GlobalVDKContext.vContext.KeepAlive();
                }
                catch(System.Exception e)
                {
                    Debug.Log("keepalive failed: " + e.ToString());
                }
                Thread.Sleep(30000);
            }
        }

        /*
         *Logs the time until the render licens expires to the console every second
         */
        public void LogLicenseStatus() {
            while (true)
            {
                try
                {
                    vdkLicenseInfo info = new vdkLicenseInfo();
                    GlobalVDKContext.vContext.GetLicenseInfo(LicenseType.Render, ref info);
                    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
                    ulong cur_time = (ulong)(System.DateTime.UtcNow - epochStart).TotalSeconds;
                    UnityEngine.Debug.Log("Render License Expiry: " + (info.expiresTimestamp - cur_time).ToString());
                    Thread.Sleep(1000);
                }
                catch {
                    continue;
                }
            }
        }
        ~VDKSessionThreadManager() {
            foreach (Thread thread in activeThreads)
                thread.Abort();
        }
    }
}


