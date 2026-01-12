using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_Player : MonoBehaviour {
	public int hp = 3;

	private void Start() {
		GameSystem.singletone.Test_Add_Player(gameObject);
	}

	private void OnTriggerEnter(Collider other) {
		if(other.CompareTag("DeadZone")) {
			if(hp > 1) {
				transform.position = new Vector3(0f, 1f, 0f);
			} else {
				GameSystem.singletone.Game_Over_Player(gameObject);
			}
			hp += -1;
		}
	}
}
