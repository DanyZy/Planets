﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorGenerator
{
    private ColorSettings settings;
    private Texture2D texture;
    const int textureResolution = 50;
    private INoiseFilter biomeNoiseFilter;

    public void UpdateSettings(ColorSettings settings)
    {
        this.settings = settings;
        if (texture == null || texture.height != settings.bioneColorSettings.biomes.Length)
        {
            texture = new Texture2D(textureResolution * 2, settings.bioneColorSettings.biomes.Length, TextureFormat.ARGB32, false);
        }

        biomeNoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.bioneColorSettings.noise);
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMaterial.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }

    public float BiomePrecentFromPoint(Vector3 pointOnUnitSphere)
    {
        float heightPercent = (pointOnUnitSphere.y + 1) / 2f;
        heightPercent += (biomeNoiseFilter.Evaluate(pointOnUnitSphere) - settings.bioneColorSettings.noiseOffset) * settings.bioneColorSettings.noiseStrength;
        float biomeIndex = 0;
        int numBiomes = settings.bioneColorSettings.biomes.Length;
        float blendRange = settings.bioneColorSettings.blendAmount / 2f + 0.001f;

        for (int i = 0; i < numBiomes; i++)
        {
            float distance = heightPercent - settings.bioneColorSettings.biomes[i].startHeight;
            float weight = Mathf.InverseLerp(-blendRange, blendRange, distance);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;
        }

        return biomeIndex / Mathf.Max(1, numBiomes - 1);
    }

    public void UpdateColors()
    {
        Color[] colors = new Color[texture.width * texture.height];
        int colorIndex = 0;
        foreach (var biome in settings.bioneColorSettings.biomes)
        {
            for (int i = 0; i < textureResolution * 2; i++)
            {
                Color gradientColor;
                if (i < textureResolution)
                {
                    gradientColor = settings.oceanColor.Evaluate(i / (textureResolution - 1f));
                }
                else
                {
                    gradientColor = biome.gradient.Evaluate((i - textureResolution) / (textureResolution - 1f));
                }
                Color tintColor = biome.tint;
                colors[colorIndex] = gradientColor * (1 - biome.tintPercent) + tintColor * biome.tintPercent;
                colorIndex++;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        settings.planetMaterial.SetTexture("_texture", texture);
    }
}
