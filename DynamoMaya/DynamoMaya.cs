using System;
using System.Windows.Forms;
using Autodesk.Maya.Runtime;
using Autodesk.Maya.OpenMaya;
using System.ServiceModel;
using System.Xml;

[assembly: ExtensionPlugin(typeof(DynamoMaya.Plugin))]

namespace DynamoMaya {

    public class Plugin : IExtensionPlugin
    {

        public ServiceHost sh
        {
            get;
            set;
        }

        public bool InitializePlugin()
        {
            // establish IPC
            try
            {
                sh = new ServiceHost(typeof(DynamoMaya.Service.ServiceImplementation));
                NetTcpBinding binding = new NetTcpBinding();
                binding.MaxBufferSize = System.Int32.MaxValue;
                binding.MaxReceivedMessageSize = System.Int32.MaxValue;
                binding.ReaderQuotas = XmlDictionaryReaderQuotas.Max;
                sh.AddServiceEndpoint(typeof(DynamoMaya.Contract.IService), binding, "net.tcp://localhost:8000");
                sh.Open();
            }
            catch {
                MessageBox.Show("Couldn't establish IPC. No communication from or to Dynamo will be available.");
            }
            return true;
        }

        public bool UninitializePlugin() {
            // close IPC
            sh.Close();
            return true;
        }

        public string GetMayaDotNetSdkBuildVersion() {
            return "";
        }

    }

}
