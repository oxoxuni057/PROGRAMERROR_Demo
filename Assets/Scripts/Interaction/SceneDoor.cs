using UnityEngine;

public class SceneDoor : Interactable   // Interactable을 물려받음 → E키 조사 기능이 그대로 있음
{
    [SerializeField] string targetScene;                 // 이동할 씬 이름
    [SerializeField] string targetSpawnId = "default";   // 그 씬에서 나타날 SpawnPoint 이름

    // 문 연결 표(SceneLinks)에서 코드로 만들 때
    public void Setup(string scene, string spawnId)
    {
        targetScene = scene;
        targetSpawnId = spawnId;
    }

    public override void Interact(PlayerController player)
    {
        base.Interact(player);   // 원래 Interactable 기능(로그, On Interact)도 실행
        SceneTransition.Instance.LoadScene(targetScene, targetSpawnId);
    }
}