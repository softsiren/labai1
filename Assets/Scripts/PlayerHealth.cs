using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Visual Feedback")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.15f;

    private Renderer[] playerRenderers;
    private List<Color> originalColors = new List<Color>();

    // Идентификаторы свойств шейдеров (для URP и Built-in)
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    void Start()
    {
        currentHealth = maxHealth;

        // Находим абсолютно все Renderer'ы на объекте и его дочерних элементах
        playerRenderers = GetComponentsInChildren<Renderer>();

        // Сохраняем исходные цвета каждого найденного материала
        foreach (var r in playerRenderers)
        {
            if (r.material.HasProperty(BaseColorID))
                originalColors.Add(r.material.GetColor(BaseColorID));
            else if (r.material.HasProperty(ColorID))
                originalColors.Add(r.material.GetColor(ColorID));
            else
                originalColors.Add(Color.white);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Игрок получил урон: {damage}. Осталось здоровья: {currentHealth}");

        if (playerRenderers != null && playerRenderers.Length > 0)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRedRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRedRoutine()
    {
        // Красим все меши в красный цвет
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            SetMaterialColor(playerRenderers[i].material, damageColor);
        }

        yield return new WaitForSeconds(flashDuration);

        // Возвращаем родные цвета
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            SetMaterialColor(playerRenderers[i].material, originalColors[i]);
        }
    }

    private void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty(BaseColorID))
            mat.SetColor(BaseColorID, color);

        if (mat.HasProperty(ColorID))
            mat.SetColor(ColorID, color);
    }

    private void Die()
    {
        Debug.Log("Игрок погиб!");
    }
}