using System;
using System.ServiceModel;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Autodesk.Maya.OpenMaya;

namespace DynamoMaya.Contract
{

    public enum MFnType
    {
        kNurbsCurve = MFn.Type.kNurbsCurve,
        kMesh = MFn.Type.kMesh,
        kParticle = MFn.Type.kParticle
    }

    public enum MFnNurbsCurveForm
    {
        kClosed = MFnNurbsCurve.Form.kClosed,
        kOpen = MFnNurbsCurve.Form.kOpen
    }

    [ServiceContract]
    public interface IService
    {

        [OperationContract]
        List<string> getMayaNodesByType(MFnType t);

        [OperationContract]
        void sendCurveToMaya(string node_name, Point3DCollection controlVertices, List<double> knots, int degree, MFnNurbsCurveForm form);

        [OperationContract]
        void receiveCurveFromMaya(string node_name, out Point3DCollection controlVertices, out List<double> weights, out List<double> knots, out int degree, out bool closed, out bool rational);

        [OperationContract]
        Point3DCollection receiveVertexPositionsFromMaya(string node_name);

    }

}
