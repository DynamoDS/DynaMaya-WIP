using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo;
using System.Windows.Forms;
using Dynamo.Models;
using Microsoft.FSharp.Collections;
using System.ServiceModel;
using Autodesk.Maya.OpenMaya;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using DynamoMaya.Contract;
using System.Xml;

namespace Dynamo.Nodes
{

    public class MayaCommunication
    {
        public static DynamoMaya.Contract.IService openChannelToMaya()
        {
            ChannelFactory<DynamoMaya.Contract.IService> cf;
            NetTcpBinding binding = new NetTcpBinding();
            binding.MaxBufferSize = System.Int32.MaxValue;
            binding.MaxReceivedMessageSize = System.Int32.MaxValue;
            binding.ReaderQuotas = XmlDictionaryReaderQuotas.Max;
            cf = new ChannelFactory<DynamoMaya.Contract.IService>(binding, "net.tcp://localhost:8000");
            return cf.CreateChannel();
        }

        public static void closeChannelToMaya(DynamoMaya.Contract.IService s)
        {
            (s as ICommunicationObject).Close();
        }

    }

    [NodeName("Maya NURBS Curve Receiver")]
    [NodeCategory(BuiltinNodeCategories.IO_NETWORK)]
    [NodeDescription("Receive Maya NURBS curves data.")]
    [IsInteractive(true)]
    public class MayaNurbsCurveReceiver : DropDrownBase
    {
        
        public MayaNurbsCurveReceiver()
        {
            OutPortData.Add(new PortData("CV", "The NURBS curve data received from Maya.", typeof(FScheme.Value.Container)));
            RegisterAllPorts();
            PopulateItems();
        }
        
        public override void PopulateItems()
        {
            Items.Clear();
            List<string> lMayaNurbsCurves = new List<string>();
            try
            {
                DynamoMaya.Contract.IService s = MayaCommunication.openChannelToMaya();
                lMayaNurbsCurves = s.getMayaNodesByType(MFnType.kNurbsCurve);
                MayaCommunication.closeChannelToMaya(s);
            }
            finally
            {
                foreach (string c in lMayaNurbsCurves)
                {
                    Items.Add(new DynamoDropDownItem(c, new object()));
                }
            }
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            if (SelectedIndex < 0)
            {
                throw new Exception("Please select a nurbsCurve node.");
            }

            var node_name = Items[SelectedIndex].Name;

            Point3DCollection controlVertices;
            List<double> weights, knots;
            int degree;
            bool closed, rational;
            DynamoMaya.Contract.IService s = MayaCommunication.openChannelToMaya();
            s.receiveCurveFromMaya(node_name, out controlVertices, out weights, out knots, out degree, out closed, out rational);
            MayaCommunication.closeChannelToMaya(s);
            
            List<XYZ> controlPoints = new List<XYZ>();
            foreach (Point3D cv in controlVertices)
            {
                controlPoints.Add(new XYZ(cv.X, cv.Y, cv.Z));
            }
            
            NurbSpline ns = NurbSpline.Create(controlPoints, weights, knots, degree, closed, true);

            return FScheme.Value.NewContainer(ns);
        }

    }

    [NodeName("Maya NURBS Curve Sender")]
    [NodeCategory(BuiltinNodeCategories.IO_NETWORK)]
    [NodeDescription("Send NURBS curve data to Maya.")]
    [IsInteractive(true)]
    public class MayaNurbsCurveSender : DropDrownBase
    {

        public MayaNurbsCurveSender()
        {
            InPortData.Add(new PortData("CV", "The NURBS curve data to send to Maya.", typeof(FScheme.Value.Container)));
            OutPortData.Add(new PortData("", "", typeof(FScheme.Value.Dummy)));
            RegisterAllPorts();
            PopulateItems();
        }

        public override void PopulateItems()
        {
            Items.Clear();
            List<string> lMayaNurbsCurves = new List<string>();
            try
            {
                DynamoMaya.Contract.IService s = MayaCommunication.openChannelToMaya();
                lMayaNurbsCurves = s.getMayaNodesByType(MFnType.kNurbsCurve);
                MayaCommunication.closeChannelToMaya(s);
            }
            finally
            {
                foreach (string c in lMayaNurbsCurves)
                {
                    Items.Add(new DynamoDropDownItem(c, new object()));
                }
            }
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            if (SelectedIndex < 0)
            {
                throw new Exception("Please select a nurbsCurve node.");
            }


            var node_name = Items[SelectedIndex].Name;
            var curve = (NurbSpline)((FScheme.Value.Container)args[0]).Item;
            
            Point3DCollection controlPoints = new Point3DCollection();
            foreach (XYZ cpt in curve.CtrlPoints)
            {
                controlPoints.Add(new Point3D(cpt.X, cpt.Y, cpt.Z));
            }

            List<double> knots = new List<double>();
            foreach(double d in curve.Knots) {
                knots.Add(d);
            }

            MFnNurbsCurveForm form = curve.isClosed ? MFnNurbsCurveForm.kClosed : MFnNurbsCurveForm.kOpen;

            DynamoMaya.Contract.IService s = MayaCommunication.openChannelToMaya();
            s.sendCurveToMaya(node_name, controlPoints, knots, curve.Degree, form);

            MayaCommunication.closeChannelToMaya(s);

            return FScheme.Value.NewDummy(node_name);
        }

    }

    [NodeName("Maya Mesh")]
    [NodeCategory(BuiltinNodeCategories.IO_NETWORK)]
    [NodeDescription("Send/Receive Maya Meshes.")]
    [IsInteractive(true)]
    public class MayaMesh : DropDrownBase
    {

        public MayaMesh()
        {
            //InPortData.Add(new PortData("CV", "The NURBS spline to send to Maya.", typeof(FScheme.Value.Container)));
            OutPortData.Add(new PortData("XYZs", "The vertex positions from a Maya mesh as XYZs.", typeof(FScheme.Value.List)));
            RegisterAllPorts();
            PopulateItems();
        }

        public override void PopulateItems()
        {
            Items.Clear();
            List<string> lMayaMeshes = new List<string>();
            try
            {
                DynamoMaya.Contract.IService s = MayaCommunication.openChannelToMaya();
                lMayaMeshes = s.getMayaNodesByType(MFnType.kMesh);
                MayaCommunication.closeChannelToMaya(s);
            }
            finally
            {
                foreach (string c in lMayaMeshes)
                {
                    Items.Add(new DynamoDropDownItem(c, new object()));
                }
            }
        }


        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            if (SelectedIndex < 0)
            {
                throw new Exception("Please select a mesh node.");
            }

            var node_name = Items[SelectedIndex].Name;

            //             if (InPorts[0].IsConnected)
            //             {
            //                 // send the to the connected node
            //                 return FScheme.Value.NewString(node_name);
            //             }

            var result = FSharpList<FScheme.Value>.Empty;

            if (OutPorts[0].IsConnected)
            {
                // get the data from the connected node
                DynamoMaya.Contract.IService s = MayaCommunication.openChannelToMaya();
                Point3DCollection vertices = s.receiveVertexPositionsFromMaya(node_name);

                foreach (Point3D v in vertices)
                {
                    XYZ pt = new XYZ(v.X, v.Y, v.Z);
                    result = FSharpList<FScheme.Value>.Cons(FScheme.Value.NewContainer(pt), result);
                }

                MayaCommunication.closeChannelToMaya(s);
            }

            return FScheme.Value.NewList(ListModule.Reverse(result));
        }

    }

}
