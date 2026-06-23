using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CircleController : MonoBehaviour
{
    [SerializeField]
    private float moveInterval = Constants.CIRCLE_MOVE_INTERVAL;

    private Camera mainCamera;
    private System.Action onClickCallback;
    private bool isPaused = false;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Initialize(System.Action clickCallback)
    {
        onClickCallback = clickCallback;

        MoveToRandomPosition();

        AutoMoveAsync(this.destroyCancellationToken).Forget();
    }

    private async UniTaskVoid AutoMoveAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(moveInterval),
                ignoreTimeScale: false,
                cancellationToken: token
            );

            if (!isPaused)
            {
                MoveToRandomPosition();
            }
        }
    }

    private void MoveToRandomPosition()
    {
        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;

        float margin = Constants.SCREEN_MARGIN / 100f;
        float randomX = Random.Range(-screenWidth / 2f + margin, screenWidth / 2f - margin);
        float randomY = Random.Range(-screenHeight / 2f + margin, screenHeight / 2f - margin);

        transform.position = new Vector3(randomX, randomY, 0);
    }

    private void OnMouseDown()
    {
        onClickCallback?.Invoke();
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Resume()
    {
        isPaused = false;
    }

    private void OnDestroy()
    {
    }
}
