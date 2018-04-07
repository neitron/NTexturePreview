﻿using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx
{
	public class NTexturePreviewBase : Editor {
		protected enum TextureUsageMode
		{
    
			Default = 0,
    
			BakedLightmapDoubleLDR = 1,
    
			BakedLightmapRGBM = 2,
    
			NormalmapDXT5nm = 3,
    
			NormalmapPlain = 4,
			RGBMEncoded = 5,
    
			AlwaysPadded = 6,
			DoubleLDR = 7,
    
			BakedLightmapFullHDR = 8,
			RealtimeLightmapRGBM = 9,
		}

		private static Material _rGBAMaterial;
		protected static Material rGBAMaterial
		{
			get
			{
				if (_rGBAMaterial == null)
					_rGBAMaterial = Resources.Load<Material>("RGBAMaterial");
				return _rGBAMaterial;
			}
		}
		
		private static Material _rGBATransparentMaterial;
		protected static Material rGBATransparentMaterial
		{
			get
			{
				if (_rGBATransparentMaterial == null)
					_rGBATransparentMaterial = Resources.Load<Material>("RGBATransparentMaterial");
				return _rGBATransparentMaterial;
			}
		}

		public bool m_R = true, m_G = true, m_B = true, m_A = true;
		
		protected class Styles
		{
			public readonly GUIContent smallZoom, largeZoom, alphaIcon, RGBIcon, scaleIcon;
			public readonly GUIStyle previewButton, previewSlider, previewSliderThumb, previewLabel;
			public readonly GUIStyle previewButton_R, previewButton_G, previewButton_B;

			public readonly GUIContent wrapModeLabel = TrTextContent("Wrap Mode");
			public readonly GUIContent wrapU = TrTextContent("U axis");
			public readonly GUIContent wrapV = TrTextContent("V axis");
			public readonly GUIContent wrapW = TrTextContent("W axis");

			public readonly GUIContent[] wrapModeContents =
			{
				TrTextContent("Repeat"),
				TrTextContent("Clamp"),
				TrTextContent("Mirror"),
				TrTextContent("Mirror Once"),
				TrTextContent("Per-axis")
			};
			public readonly int[] wrapModeValues =
			{
				(int)TextureWrapMode.Repeat,
				(int)TextureWrapMode.Clamp,
				(int)TextureWrapMode.Mirror,
				(int)TextureWrapMode.MirrorOnce,
				-1
			};

			public Styles()
			{
				smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
				largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
				alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
				scaleIcon = EditorGUIUtility.IconContent("ViewToolZoom On");
				RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
				previewButton = "preButton";
				previewSlider = "preSlider";
				previewSliderThumb = "preSliderThumb";
				previewLabel = new GUIStyle("preLabel")
				{
					// UpperCenter centers the mip icons vertically better than MiddleCenter
					alignment = TextAnchor.UpperCenter
				};
				previewButton_R = new GUIStyle(previewButton)
				{
					padding =  new RectOffset(5,5,0,0),
					alignment = TextAnchor.MiddleCenter,
					normal = {textColor = new Color(1f, 0.28f, 0.33f)}
				};
				previewButton_G = new GUIStyle(previewButton_R) {normal = {textColor = new Color(0.45f, 1f, 0.28f)}};
				previewButton_B = new GUIStyle(previewButton_R) {normal = {textColor = new Color(0f, 0.65f, 1f)}};
			}

			private static MethodInfo _TrTextContent;
			private static GUIContent TrTextContent(string s)
			{
				if (_TrTextContent == null)
					_TrTextContent = typeof(EditorGUIUtility).GetMethod("TrTextContent", BindingFlags.NonPublic | BindingFlags.Static, null, new[]{typeof(string), typeof(string), typeof(Texture)}, null);
				return (GUIContent)_TrTextContent.Invoke(null, new object[] {s, null, null});
			}
		}
		protected static Styles s_Styles;
		
		protected static void DrawRect(Rect rect)
		{
			GL.Vertex(new Vector3(rect.xMin, rect.yMin, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMin, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMin, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMin, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMin, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMin, rect.yMin, 0f));
		}
	}
}