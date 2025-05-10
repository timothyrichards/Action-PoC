using UnityEngine;

namespace Scripts.Misc
{
    public class LookAtCamera : MonoBehaviour
    {
        private GameObject _target;

        private void Update()
        {
            if (_target)
            {
                var newRotation = transform.position - _target.transform.position;
                transform.rotation = Quaternion.LookRotation(newRotation, Vector3.up);
            }
            else
            {
                _target = Camera.main.gameObject;
            }
        }
    }
}
