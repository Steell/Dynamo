﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Dynamo.Models;
using Dynamo.Utilities;

using ProtoCore.AST.AssociativeAST;

namespace Dynamo.Nodes
{
    /// <summary>
    ///     Controller that synchronizes a node with a custom node definition.
    /// </summary>
    public class CustomNodeController : FunctionCallNodeController
    {
        public CustomNodeController(CustomNodeDefinition def) : base(def) { }

        /// <summary>
        ///     Definition of a custom node.
        /// </summary>
        public new CustomNodeDefinition Definition
        {
            get { return base.Definition as CustomNodeDefinition; }
            internal set { base.Definition = value; }
        }

        protected override void InitializeInputs(NodeModel model)
        {
            model.InPortData.Clear();

            if (Definition.Parameters == null) return;

            foreach (string arg in Definition.Parameters)
                model.InPortData.Add(new PortData(arg, "parameter"));
        }

        protected override void InitializeOutputs(NodeModel model)
        {
            model.OutPortData.Clear();
            if (Definition.ReturnKeys != null && Definition.ReturnKeys.Any())
            {
                foreach (string key in Definition.ReturnKeys)
                    model.OutPortData.Add(new PortData(key, "return value"));
            }
            else
                model.OutPortData.Add(new PortData("", "return value"));
        }

        protected override AssociativeNode GetFunctionApplication(NodeModel model, List<AssociativeNode> inputAstNodes)
        {
            if (!model.IsPartiallyApplied)
                return AstFactory.BuildFunctionCall(Definition.FunctionName, inputAstNodes);

            var count = Definition.Parameters.Count();
            return AstFactory.BuildFunctionObject(
                Definition.FunctionName,
                count,
                Enumerable.Range(0, count).Where(model.HasInput),
                inputAstNodes);
        }

        protected override void BuildAstForPartialMultiOutput(
            NodeModel model, AssociativeNode rhs, List<AssociativeNode> resultAst)
        {
            base.BuildAstForPartialMultiOutput(model, rhs, resultAst);

            var emptyList = AstFactory.BuildExprList(new List<AssociativeNode>());
            var previewIdInit = AstFactory.BuildAssignment(model.AstIdentifierForPreview, emptyList);

            resultAst.Add(previewIdInit);
            resultAst.AddRange(
                Definition.ReturnKeys.Select(
                    (rtnKey, idx) =>
                        AstFactory.BuildAssignment(
                            AstFactory.BuildIdentifier(
                                model.AstIdentifierForPreview.Name,
                                AstFactory.BuildStringNode(rtnKey)),
                            model.GetAstIdentifierForOutputIndex(idx))));
        }

        protected override void AssignIdentifiersForFunctionCall(
            NodeModel model, AssociativeNode rhs, List<AssociativeNode> resultAst)
        {
            if (model.OutPortData.Count == 1)
            {
                resultAst.Add(AstFactory.BuildAssignment(model.AstIdentifierForPreview, rhs));
                resultAst.Add(
                    AstFactory.BuildAssignment(
                        model.GetAstIdentifierForOutputIndex(0),
                        model.AstIdentifierForPreview));
            }
            else
                base.AssignIdentifiersForFunctionCall(model, rhs, resultAst);
        }

        public override void SyncNodeWithDefinition(NodeModel model)
        {
            if (!IsInSyncWithNode(model))
            {
                model.DisableReporting();
                base.SyncNodeWithDefinition(model);
                model.EnableReporting();
                model.RequiresRecalc = true;
            }
        }

        public override void SaveNode(XmlDocument xmlDoc, XmlElement nodeElement, SaveContext saveContext)
        {
            //Debug.WriteLine(pd.Object.GetType().ToString());
            XmlElement outEl = xmlDoc.CreateElement("ID");

            outEl.SetAttribute("value", Definition.FunctionId.ToString());
            nodeElement.AppendChild(outEl);
        }

        public override void LoadNode(XmlNode nodeElement)
        {
            XmlNode idNode =
                nodeElement.ChildNodes.Cast<XmlNode>()
                    .LastOrDefault(subNode => subNode.Name.Equals("ID"));

            if (idNode == null || idNode.Attributes == null) return;
            
            string id = idNode.Attributes[0].Value;
            
            Guid funcId;
            if (!Guid.TryParse(id, out funcId) && nodeElement.Attributes != null)
            {
                funcId = GuidUtility.Create(
                    GuidUtility.UrlNamespace,
                    nodeElement.Attributes["nickname"].Value);
            }
            
            if (!VerifyFuncId(ref funcId))
                LoadProxyCustomNode(funcId);
            
            Definition = dynSettings.Controller.CustomNodeManager.GetFunctionDefinition(funcId);

            if (Definition.IsProxy)
                throw new Exception("Cannot load custom node");
        }

        /// <summary>
        ///   Return if the custom node instance is in sync with its definition.
        ///   It may be out of sync if .dyf file is opened and updated and then
        ///   .dyn file is opened. 
        /// </summary>
        public bool IsInSyncWithNode(NodeModel model)
        {
            return Definition != null
                && ((Definition.Parameters == null
                    || (Definition.Parameters.Count() == model.InPortData.Count()
                        && Definition.Parameters.SequenceEqual(
                            model.InPortData.Select(p => p.NickName))))
                    && (Definition.ReturnKeys == null
                        || Definition.ReturnKeys.Count() == model.OutPortData.Count()
                            && Definition.ReturnKeys.SequenceEqual(
                                model.OutPortData.Select(p => p.NickName))));
        }

        private bool VerifyFuncId(ref Guid funcId)
        {
            if (funcId == null) return false;

            // if the dyf does not exist on the search path...
            if (dynSettings.Controller.CustomNodeManager.Contains(funcId))
                return true;

            CustomNodeManager manager = dynSettings.Controller.CustomNodeManager;

            // if there is a node with this name, use it instead
            if (!manager.Contains(NickName)) return false;

            funcId = manager.GetGuidFromName(NickName);
            return true;
        }

        private void LoadProxyCustomNode(Guid funcId)
        {
            var proxyDef = new CustomNodeDefinition(funcId)
            {
                WorkspaceModel =
                    new CustomNodeWorkspaceModel(NickName, "Custom Nodes") { FileName = null },
                IsProxy = true
            };

            string userMsg = "Failed to load custom node: " + NickName + ".  Replacing with proxy custom node.";

            dynSettings.DynamoLogger.Log(userMsg);

            // tell custom node loader, but don't provide path, forcing user to resave explicitly
            dynSettings.Controller.CustomNodeManager.SetFunctionDefinition(funcId, proxyDef);
        }
    }
}