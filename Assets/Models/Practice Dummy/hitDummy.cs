using UnityEngine;
using System.Collections;

public class hitDummy : MonoBehaviour
{
	public EnemyEntity enemy;
	public GameObject hay;
	private Animator DummyAnimator;
	private IHealth healthComponent;

	void Start()
	{
		DummyAnimator = GetComponent<Animator>();
		healthComponent = enemy.GetComponent<HealthComponent>();
	}

	void OnEnable()
	{
		healthComponent.OnHealthChanged += OnHealthChanged;
	}

	void OnDisable()
	{
		healthComponent.OnHealthChanged -= OnHealthChanged;
	}

	void OnHealthChanged(float health)
	{
		DummyAnimator.SetTrigger("hit");
		GameObject hayInstance = Instantiate(hay, transform.position + Vector3.up, Quaternion.identity);
		StartCoroutine(DestroyHayAfterDelay(hayInstance, 1f));
	}

	IEnumerator DestroyHayAfterDelay(GameObject hayObj, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (hayObj != null)
		{
			Destroy(hayObj);
		}
	}
}
