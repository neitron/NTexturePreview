﻿using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vertx
{
	[CustomEditor(typeof(Texture2D), true), CanEditMultipleObjects]
	public class NTexture2DPreview : NTexturePreview {
		Editor defaultEditor;
		private Texture2D _texture2D;
		void OnEnable(){
			//When this inspector is created, also create the built-in inspector
			defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.TextureInspector, UnityEditor"));
			_texture2D = (Texture2D)target;
		}

		[SerializeField]
		float m_MipLevel;
		
		[SerializeField]
		protected Vector2 m_Pos;
		
		void OnDisable(){
			//When OnDisable is called, the default editor we created should be destroyed to avoid memory leakage.
			//Also, make sure to call any required methods like OnDisable
			MethodInfo disableMethod = defaultEditor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (disableMethod != null)
				disableMethod.Invoke(defaultEditor,null);
			DestroyImmediate(defaultEditor);
		}

		public override void OnInspectorGUI()
		{
			defaultEditor.OnInspectorGUI();
		}

		public override bool HasPreviewGUI()
		{
			return target != null;
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            // show texture
            Texture t = target as Texture;
            if (t == null) // texture might be gone by now, in case this code is used for floating texture preview
                return;

            // Render target must be created before we can display it (case 491797)
            RenderTexture rt = t as RenderTexture;
            if (rt != null)
            {
                if (!SystemInfo.SupportsRenderTextureFormat(rt.format))
                    return; // can't do this RT format
                rt.Create();
            }

            if (IsCubemap())
            {
	            //TODO perhaps support custom cubemap settings. Not currently!
                defaultEditor.OnPreviewGUI(r, background);
                return;
            }

            // target can report zero sizes in some cases just after a parameter change;
            // guard against that.
            int texWidth = Mathf.Max(t.width, 1);
            int texHeight = Mathf.Max(t.height, 1);

            float mipLevel = GetMipLevelForRendering();
            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUIBeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");
            FilterMode oldFilter = t.filterMode;
            SetFilterModeNoDirty(t, FilterMode.Point);

            Texture2D t2d = t as Texture2D;
            if (m_ShowAlpha)
                EditorGUI.DrawTextureAlpha(wantedRect, t, ScaleMode.StretchToFill, 0, mipLevel);
            else if (t2d != null && t2d.alphaIsTransparency)
                EditorGUI.DrawTextureTransparent(wantedRect, t, ScaleMode.StretchToFill, 0, mipLevel);
            else
                EditorGUI.DrawPreviewTexture(wantedRect, t, null, ScaleMode.StretchToFill, 0, mipLevel);

            // TODO: Less hacky way to prevent sprite rects to not appear in smaller previews like icons.
            if (wantedRect.width > 32 && wantedRect.height > 32)
            {
                string path = AssetDatabase.GetAssetPath(t);
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                SpriteMetaData[] spritesheet = textureImporter != null ? textureImporter.spritesheet : null;

                if (spritesheet != null && textureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    Rect screenRect = new Rect();
                    Rect sourceRect = new Rect();
                    GUICalculateScaledTextureRects(wantedRect, ScaleMode.StretchToFill, t.width / (float)t.height, ref screenRect, ref sourceRect);

                    int origWidth = t.width;
                    int origHeight = t.height;
	                TextureImporterGetWidthAndHeight(textureImporter, ref origWidth, ref origHeight);
                    float definitionScale = t.width / (float)origWidth;

                    ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                    GL.Color(new Color(1f, 1f, 1f, 0.5f));
                    foreach (SpriteMetaData sprite in spritesheet)
                    {
                        Rect spriteRect = sprite.rect;
                        Rect spriteScreenRect = new Rect();
                        spriteScreenRect.xMin = screenRect.xMin + screenRect.width * (spriteRect.xMin / t.width * definitionScale);
                        spriteScreenRect.xMax = screenRect.xMin + screenRect.width * (spriteRect.xMax / t.width * definitionScale);
                        spriteScreenRect.yMin = screenRect.yMin + screenRect.height * (1f - spriteRect.yMin / t.height * definitionScale);
                        spriteScreenRect.yMax = screenRect.yMin + screenRect.height * (1f - spriteRect.yMax / t.height * definitionScale);
                        DrawRect(spriteScreenRect);
                    }
                    GL.End();
                    GL.PopMatrix();
                }
            }

            SetFilterModeNoDirty(t, oldFilter);

            m_Pos = PreviewGUIEndScrollView();
			// ReSharper disable once CompareOfFloatsByEqualityOperator
            if (mipLevel != 0)
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Mip " + mipLevel);
		}
		
		public float GetMipLevelForRendering()
		{
			if (target == null)
				return 0.0f;

			if (IsCubemap())
			{
				throw new NotImplementedException();
				//This should never be called yet by this class, and is handled by the default editor.
				//TODO support cubemap rendering here too
//				return m_CubemapPreview.GetMipLevelForRendering(target as Texture);
			}

			return Mathf.Min(m_MipLevel, GetMipmapCount(target as Texture) - 1);
		}

		public static bool IsNormalMap(Texture t)
		{
			TextureUsageMode mode = GetUsageMode(t);
			return mode == TextureUsageMode.NormalmapPlain || mode == TextureUsageMode.NormalmapDXT5nm;
		}
		
		bool IsCubemap()
		{
			var t = target as Texture;
			return t != null && t.dimension == TextureDimension.Cube;
		}

		bool IsVolume()
		{
			var t = target as Texture;
			return t != null && t.dimension == TextureDimension.Tex3D;
		}

		private bool m_ShowAlpha;
		
		public override void OnPreviewSettings()
		{
			if (IsCubemap())
            {
	            //TODO perhaps support custom cubemap settings. Not currently!
                defaultEditor.OnPreviewSettings();
                return;
            }


            if (s_Styles == null)
                s_Styles = new Styles();


            // TextureInspector code is reused for RenderTexture and Cubemap inspectors.
            // Make sure we can handle the situation where target is just a Texture and
            // not a Texture2D. It's also used for large popups for mini texture fields,
            // and while it's being shown the actual texture object might disappear --
            // make sure to handle null targets.
            Texture tex = target as Texture;
            bool showMode = true;
            bool alphaOnly = false;
            bool hasAlpha = true;
            int mipCount = 1;

            if (target is Texture2D)
            {
                alphaOnly = true;
                hasAlpha = false;
            }

            foreach (Texture t in targets)
            {
                if (t == null) // texture might have disappeared while we're showing this in a preview popup
                    continue;
                TextureFormat format = 0;
                bool checkFormat = false;
                if (t is Texture2D)
                {
                    format = (t as Texture2D).format;
                    checkFormat = true;
                }

                if (checkFormat)
                {
                    if (!IsAlphaOnlyTextureFormat(format))
                        alphaOnly = false;
                    if (HasAlphaTextureFormat(format))
                    {
	                    TextureUsageMode mode = GetUsageMode(t);
                        if (mode == TextureUsageMode.Default) // all other texture usage modes don't displayable alpha
                            hasAlpha = true;
                    }
                }

                mipCount = Mathf.Max(mipCount, GetMipmapCount(t));
            }

            if (alphaOnly)
            {
                m_ShowAlpha = true;
                showMode = false;
            }
            else if (!hasAlpha)
            {
                m_ShowAlpha = false;
                showMode = false;
            }

            if (showMode && tex != null && !IsNormalMap(tex))
                m_ShowAlpha = GUILayout.Toggle(m_ShowAlpha, m_ShowAlpha ? s_Styles.alphaIcon : s_Styles.RGBIcon, s_Styles.previewButton);

            if (mipCount > 1)
            {
                GUILayout.Box(s_Styles.smallZoom, s_Styles.previewLabel);
                GUI.changed = false;
                m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_MipLevel, mipCount - 1, 0, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.MaxWidth(64)));
                GUILayout.Box(s_Styles.largeZoom, s_Styles.previewLabel);
            }
		}

		private static Type m_TextureUtilType;
		private static Type TextureUtilType
		{
			get { return m_TextureUtilType ?? (m_TextureUtilType = Type.GetType("UnityEditor.TextureUtil, UnityEditor")); }
		}

		private static MethodInfo m_IsAlphaOnlyTextureFormat;
		private static bool IsAlphaOnlyTextureFormat(TextureFormat format)
		{
			if (m_IsAlphaOnlyTextureFormat == null)
				m_IsAlphaOnlyTextureFormat = TextureUtilType.GetMethod("IsAlphaOnlyTextureFormat", BindingFlags.Public | BindingFlags.Static);
			return (bool) m_IsAlphaOnlyTextureFormat.Invoke(null, new object[]{format});
		}
		
		private static MethodInfo m_HasAlphaTextureFormat;
		private static bool HasAlphaTextureFormat(TextureFormat format)
		{
			if (m_HasAlphaTextureFormat == null)
				m_HasAlphaTextureFormat = TextureUtilType.GetMethod("HasAlphaTextureFormat", BindingFlags.Public | BindingFlags.Static);
			return (bool) m_HasAlphaTextureFormat.Invoke(null, new object[]{format});
		}
		
		private static MethodInfo m_GetUsageMode;
		private static TextureUsageMode GetUsageMode(Texture texture)
		{
			if (m_GetUsageMode == null)
				m_GetUsageMode = TextureUtilType.GetMethod("GetUsageMode", BindingFlags.Public | BindingFlags.Static);
			return (TextureUsageMode) m_GetUsageMode.Invoke(null, new object[]{texture});
		}
		
		private static MethodInfo m_GetMipmapCount;
		private static int GetMipmapCount(Texture texture)
		{
			if (m_GetMipmapCount == null)
				m_GetMipmapCount = TextureUtilType.GetMethod("GetMipmapCount", BindingFlags.Public | BindingFlags.Static);
			return (int) m_GetMipmapCount.Invoke(null, new object[]{texture});
		}
		
		private static MethodInfo m_SetFilterModeNoDirty;
		private static void SetFilterModeNoDirty(Texture texture, FilterMode mode)
		{
			if (m_SetFilterModeNoDirty == null)
				m_SetFilterModeNoDirty = TextureUtilType.GetMethod("SetFilterModeNoDirty", BindingFlags.Public | BindingFlags.Static);
			m_SetFilterModeNoDirty.Invoke(null, new object[]{texture, mode});
		}

		private static Type m_PreviewGUIType;
		private static Type PreviewGUIType
		{
			get { return m_PreviewGUIType ?? (m_PreviewGUIType = Type.GetType("PreviewGUI, UnityEditor")); }
		}
		
		private static MethodInfo m_PreviewGUIBeginScrollView;
		private static void PreviewGUIBeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
		{
			if (m_PreviewGUIBeginScrollView == null)
				m_PreviewGUIBeginScrollView = PreviewGUIType.GetMethod("BeginScrollView", BindingFlags.NonPublic | BindingFlags.Static);
			object[] results = {position, scrollPosition, viewRect, horizontalScrollbar, verticalScrollbar};
			m_PreviewGUIBeginScrollView.Invoke(null, results);
		}
		
		private static MethodInfo m_PreviewGUIEndScrollView;
		private static Vector2 PreviewGUIEndScrollView()
		{
			if (m_PreviewGUIEndScrollView == null)
				m_PreviewGUIEndScrollView = PreviewGUIType.GetMethod("EndScrollView", BindingFlags.Public | BindingFlags.Static);
			return (Vector2)m_PreviewGUIEndScrollView.Invoke(null, null);
		}

		private static MethodInfo m_ApplyWireMaterial;
		private static void ApplyWireMaterial()
		{
			if (m_ApplyWireMaterial == null)
				m_ApplyWireMaterial = typeof(HandleUtility).GetMethod("ApplyWireMaterial", BindingFlags.NonPublic | BindingFlags.Static);
			m_ApplyWireMaterial.Invoke(null, null);
		}

		private static MethodInfo m_TextureImporterGetWidthAndHeight;
		private static void TextureImporterGetWidthAndHeight(TextureImporter textureImporter, ref int origWidth, ref int origHeight)
		{
			if (m_TextureImporterGetWidthAndHeight == null)
				m_TextureImporterGetWidthAndHeight = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
			object[] results = {origWidth, origHeight};
			m_TextureImporterGetWidthAndHeight.Invoke(textureImporter, results);
			origWidth = (int)results[0];
			origHeight = (int)results[1];
		}

		private static MethodInfo m_GUICalculateScaledTextureRects;
		private static void GUICalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
		{
			if (m_GUICalculateScaledTextureRects == null)
				m_GUICalculateScaledTextureRects = typeof(GUI).GetMethod("CalculateScaledTextureRects", BindingFlags.NonPublic | BindingFlags.Instance);
			object[] results = {position, scaleMode, imageAspect, outScreenRect, outSourceRect};
			m_GUICalculateScaledTextureRects.Invoke(null, results);
			outScreenRect = (Rect)results[3];
			outSourceRect = (Rect)results[4];
		}
		
		
	}
}