using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu_UI_Controller : MonoBehaviour {
	public GameObject RoomCreateBtn;
	public GameObject RoomEnterBtn;
	public GameObject RoomCreate;
	public GameObject RoomEnter;

	public void RoomCreateUI_btn() {
		setActvie_menuBtn(false);
		RoomCreate.SetActive(true);
	}

	public void RoomEnterUI_btn() {
		setActvie_menuBtn(false);
		RoomEnter.SetActive(true);
	}

	public void BacktoMainMenu_btn() {
		setActvie_menuBtn(true);
		RoomCreate.SetActive(false);
		RoomEnter.SetActive(false);
	}

	private void setActvie_menuBtn(bool _bool) {
		RoomCreateBtn.SetActive(_bool);
		RoomEnterBtn.SetActive(_bool);
	}
}
