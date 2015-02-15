using System;
using System.ServiceModel;
using System.Collections.Generic;
using Autodesk.Maya.OpenMaya;
using System.Windows.Media.Media3D;
using DynamoMaya;
using DynamoMaya.Contract;

namespace DynamoMaya.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class ServiceImplementation : IService
    {

        public List<string> getMayaNodesByType(MFnType t)
        {
            List<string> lMayaNodes = new List<string>();
            MItDag itdagn = new MItDag(MItDag.TraversalType.kBreadthFirst, (MFn.Type)t);
            MFnDagNode dagn;

            while (!itdagn.isDone)
            {
                dagn = new MFnDagNode(itdagn.item());
                if(!dagn.isIntermediateObject)
                    lMayaNodes.Add(dagn.partialPathName);
                itdagn.next();
            }

            return lMayaNodes;

        }

        // get the dependency node
        MObject getDependNode(string node_name)
        {
            MSelectionList sl = new MSelectionList();
            sl.add(node_name, true);
            MObject o = new MObject();
            sl.getDependNode(0, o);
            return o;
        }

        // get the DAG node
        MDagPath getDagNode(string node_name)
        {
            MSelectionList sl = new MSelectionList();
            sl.add(node_name, true);
            MDagPath dp = new MDagPath();
            sl.getDagPath(0, dp);
            return dp;
        }

        // get the plug at the node
        MPlug getPlug(string node_name, string attribute_name)
        {
            MFnDependencyNode dn = new MFnDependencyNode(getDependNode(node_name));
            MPlug pl = dn.findPlug(attribute_name);
            return pl;
        }

        public void sendCurveToMaya(string node_name, Point3DCollection controlVertices, List<double> knots, int degree, MFnNurbsCurveForm form)
        {
            MFnDagNode dn = new MFnDagNode(getDagNode(node_name));
            MPlug plCreate = dn.findPlug("create");
            MPlug plDynamoCreate = new MPlug();

            try
            {
                plDynamoCreate = dn.findPlug("dynamoCreate");
            }
            catch
            {
                MFnTypedAttribute tAttr = new MFnTypedAttribute();
                MObject ldaDynamoCreate = tAttr.create("dynamoCreate", "dc", MFnData.Type.kNurbsCurve, MObject.kNullObj);
                try
                {
                    dn.addAttribute(ldaDynamoCreate, MFnDependencyNode.MAttrClass.kLocalDynamicAttr);
                    plDynamoCreate = dn.findPlug(ldaDynamoCreate);
                    MDagModifier dagm = new MDagModifier();
                    dagm.connect(plDynamoCreate, plCreate);
                    dagm.doIt();
                }
                catch
                {
                    return;
                }
            }

            MFnNurbsCurveData ncd = new MFnNurbsCurveData();
            MObject oOwner = ncd.create();
            MFnNurbsCurve nc = new MFnNurbsCurve();

            MPointArray p_aControlVertices = new MPointArray();
            foreach (Point3D p in controlVertices)
            {
                p_aControlVertices.Add(new MPoint(p.X, p.Y, p.Z));
            }

            MDoubleArray d_aKnots = new MDoubleArray();
            for (int i = 1; i < knots.Count - 1; ++i )
            {
                d_aKnots.Add(knots[i]);
            }

            nc.create(p_aControlVertices, d_aKnots, (uint)degree, (MFnNurbsCurve.Form)form, false, true, oOwner);

            plDynamoCreate.setMObject(oOwner);

            MGlobal.executeCommandOnIdle(String.Format("dgdirty {0}.create;", node_name));
        }


        public void receiveCurveFromMaya(string node_name, out Point3DCollection controlVertices, out List<double> weights, out List<double> knots, out int degree, out bool closed, out bool rational)
        {
            MPlug plLocal = getPlug(node_name, "local");
            MObject oLocal = new MObject();
            plLocal.getValue(oLocal);
            
            MFnNurbsCurve nc = new MFnNurbsCurve(oLocal);

            MPointArray p_aCVs = new MPointArray();
            nc.getCVs(p_aCVs, MSpace.Space.kWorld);
            controlVertices = new Point3DCollection();
            weights = new List<double>();
            foreach (MPoint p in p_aCVs)
            {
                controlVertices.Add(new Point3D(p.x, p.y, p.z));
                weights.Add(1.0);
            }

            double min = 0, max = 0;
            nc.getKnotDomain(ref min, ref max);
            MDoubleArray d_aKnots = new MDoubleArray();
            nc.getKnots(d_aKnots);

            knots = new List<double>();
            knots.Add(min);
            foreach (double d in d_aKnots)
            {
                knots.Add(d);
            }
            knots.Add(max);

            degree = nc.degree;
            closed = nc.form == MFnNurbsCurve.Form.kClosed ? true : false;
            rational = true;
        }

        public Point3DCollection receiveVertexPositionsFromMaya(string node_name)
        {
            MPlug plLocal = getPlug(node_name, "outMesh");
            MObject oOutMesh = new MObject();
            plLocal.getValue(oOutMesh);
            MFnMesh m = new MFnMesh(oOutMesh);
            MPointArray p_aVertices = new MPointArray();
            m.getPoints(p_aVertices, MSpace.Space.kWorld);
            Point3DCollection vertices = new Point3DCollection();
            foreach (MPoint p in p_aVertices)
            {
                vertices.Add(new Point3D(p.x, p.y, p.z));
            }
            return vertices;
        }
    
    }

}