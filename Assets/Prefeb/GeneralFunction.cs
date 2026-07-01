using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralFunction : MonoBehaviour {

	public static GeneralFunction intance;

	public GameObject menu;
	public GameObject myloading;

	public GameObject tutorail4, tutorail5;

	void Awake()
	{
		if (intance != null)
		{
			DestroyImmediate (this.gameObject);
		} else {
			intance = this;
			DontDestroyOnLoad (this.gameObject);
		}
	}

	public void LoadSceneByName(string sceneName)
	{
		SceneManager.LoadScene (sceneName);
	}

	public string LoadedSceneName
	{
		get { return SceneManager.GetActiveScene().name; }
	}

	public void DifficultyLevel()
	{
		tutorail4.SetActive(true);
	}
}
