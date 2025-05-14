//using UnityEngine;

//public class PlayerGoal : MonoBehaviour
//{
//    private void OnTriggerEnter2D(Collider2D other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            Debug.Log("Player reached the goal!");

//            // Show win screen
//            GameManager.Instance.ShowVictory();

//            // Log AI metrics if available
//            if (AIMetricsLogger.Instance != null)
//                AIMetricsLogger.Instance.PrintFinalStats();
//        }
//    }
//}
