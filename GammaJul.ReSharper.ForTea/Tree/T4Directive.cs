#region License
//    Copyright 2012 Julien Lebosquain
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using GammaJul.ReSharper.ForTea.Parsing;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace GammaJul.ReSharper.ForTea.Tree {

	/// <summary>
	/// Represents a directive, like <c>&lt;#@ import namespace="System"#&gt;</c>
	/// </summary>
	public class T4Directive : T4CompositeElement, IT4Directive {

		/// <summary>
		/// Gets the node type of this element.
		/// </summary>
		public override NodeType NodeType {
			get { return T4ElementTypes.T4Directive; }
		}

		/// <summary>
		/// Gets the role of a child node.
		/// </summary>
		/// <param name="nodeType">The type of the child node</param>
		protected override T4TokenRole GetChildRole(NodeType nodeType) {
			if (nodeType == T4ElementTypes.T4DirectiveAttribute)
				return T4TokenRole.Attribute;
			if (nodeType == T4TokenNodeTypes.BlockEnd)
				return T4TokenRole.BlockEnd;
			if (nodeType == T4TokenNodeTypes.DirectiveStart)
				return T4TokenRole.BlockStart;
			if (nodeType == T4TokenNodeTypes.Name)
				return T4TokenRole.Name;
			return T4TokenRole.Unknown;
		}

		/// <summary>
		/// Gets the start token of the block.
		/// </summary>
		/// <returns>A block start token.</returns>
		public IT4Token GetStartToken() {
			return (IT4Token) this.GetChildByRole((short) T4TokenRole.BlockStart);
		}

		/// <summary>
		/// Gets the token representing the name of this node.
		/// </summary>
		/// <returns>The name token, or <c>null</c> if none is available.</returns>
		public IT4Token GetNameToken() {
			return (IT4Token) FindChildByRole((short) T4TokenRole.Name);
		}

		/// <summary>
		/// Gets the end token of the block.
		/// </summary>
		/// <returns>A block end token, or <c>null</c> if none is available.</returns>
		public IT4Token GetEndToken() {
			return (IT4Token) FindChildByRole((short) T4TokenRole.BlockEnd);
		}

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		/// <returns>The node name, or <c>null</c> if none is available.</returns>
		public string GetName() {
			IT4Token nameToken = GetNameToken();
			return nameToken != null ? nameToken.GetText() : null;
		}

		/// <summary>
		/// Returns the attributes of the directive.
		/// </summary>
		public IEnumerable<IT4DirectiveAttribute> GetAttributes() {
			return FindChildrenByRole<IT4DirectiveAttribute>((short) T4TokenRole.Attribute);
		}

		/// <summary>
		/// Returns an attribute that has a given name.
		/// </summary>
		/// <param name="name">The name of the attribute to retrieve.</param>
		/// <returns>An instance of <see cref="IT4DirectiveAttribute"/>, or <c>null</c> if none had the name <paramref name="name"/>.</returns>
		public IT4DirectiveAttribute GetAttribute(string name) {
			return String.IsNullOrEmpty(name)
				? null
				: GetAttributes().FirstOrDefault(att => name.Equals(att.GetName(), StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsEndNode([NotNull] ITreeNode node) {
			if (node.GetTokenType() == T4TokenNodeTypes.BlockEnd)
				return true;
			var missingTokenErrorElement = node as MissingTokenErrorElement;
			return missingTokenErrorElement != null && missingTokenErrorElement.MissingTokenType == MissingTokenType.BlockEnd;
		}

		/// <summary>
		/// Adds a new attribute to this directive.
		/// </summary>
		/// <param name="attribute">The attribute to add.</param>
		/// <returns>A new instance of <see cref="IT4DirectiveAttribute"/>, representing <paramref name="attribute"/> in the T4 file.</returns>
		public IT4DirectiveAttribute AddAttribute(IT4DirectiveAttribute attribute) {
			if (attribute == null)
				throw new ArgumentNullException("attribute");

			using (WriteLockCookie.Create(IsPhysical())) {

				ITreeNode lastNode = LastChild;
				Assertion.AssertNotNull(lastNode, "lastNode != null");

				ITreeNode anchor = IsEndNode(lastNode) ? lastNode.PrevSibling : lastNode;
				Assertion.AssertNotNull(anchor, "anchor != null");
				bool addSpaceAfter = anchor.GetTokenType() == T4TokenNodeTypes.Space;
				bool addSpaceBefore = !addSpaceAfter;

				if (addSpaceBefore)
					anchor = ModificationUtil.AddChildAfter(anchor, T4TokenNodeTypes.Space.CreateLeafElement());

				IT4DirectiveAttribute result = ModificationUtil.AddChildAfter(anchor, attribute);

				if (addSpaceAfter)
					ModificationUtil.AddChildAfter(result, T4TokenNodeTypes.Space.CreateLeafElement());

				return result;
			}
		}

		public override string ToString() {
			return GetText();
		}

	}

}