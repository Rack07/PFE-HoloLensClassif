﻿using HoloToolkit.Unity;

using System.Collections;

using UnityEngine;

public class GlobalActions : Singleton<GlobalActions> {

	public GameObject	GlobalInput;
	public Texture2D	DefaultTexture;

	private GlobalInputReceiver globalInputReceiver;

	private bool photoMode = true;

	private bool updatingDocument = false;
	private GameObject document;

	void Start() {
		this.globalInputReceiver = GlobalInput.GetComponent<GlobalInputReceiver>();
		this.globalInputReceiver.OnSingleTap += SingleTap;
		this.globalInputReceiver.OnDoubleTap += DoubleTap;
	}

	void SingleTap(float delay) {
		// We need to use a coroutine to way for the delay before invoking single tap.
		// This will let some time to the user to perform a double tap.
		StartCoroutine(WaitForDoubleTap(delay));
	}

	IEnumerator WaitForDoubleTap(float delay) {
		yield return new WaitForSeconds(delay);
		if (this.globalInputReceiver.SingleTapped && photoMode) {
			Debug.Log("Single tap");
			// This should take a photo, send it to the server which will check if its valid crop and unwrap it and send it back. Then call the instanciator with this new photo a create a document and add it to the document panel.
			AudioPlayer.Instance.PlayClip(AudioPlayer.Instance.Photo);
			if (PhotoCapturer.Instance.HasFoundCamera) {
				PhotoCapturer.Instance.TakePhoto(OnPhotoTaken);
			}
			else {
				Resolution defaultTextureResolution = new Resolution {
					width = DefaultTexture.width,
					height = DefaultTexture.height
				};

				OnPhotoTaken(this.DefaultTexture, defaultTextureResolution);
			}
		}
	}

	void DoubleTap() {
		Debug.Log("Double tap" + this.globalInputReceiver.SingleTapped + ";" + this.photoMode);
		// This should toogle (open/close) the document viewer panel & deactivate/activate the single tap event.
		// We need to deactivate the single tap event so that if we miss the documents, we won't take a photo while in
		// document view mode.
		if (DocumentCollection.Instance.DocumentsCount() > 0) {
			DocumentCollection.Instance.Toggle();
			this.photoMode = !DocumentCollection.Instance.IsActive();
			this.updatingDocument = false;
		}
	}

	public void OnPhotoTaken(Texture2D photo, Resolution res) {
		this.photoMode = false;

#if USE_SERVER
		CameraFrame frame = new CameraFrame(res, photo.GetPixels32());
#else
		Texture2D croppedPhoto = new Texture2D(photo.width, photo.height);
		croppedPhoto.SetPixels32(photo.GetPixels32());
		croppedPhoto.Apply();
#endif

		if (updatingDocument) {
#if USE_SERVER
			RequestLauncher.Instance.UpdateDocumentPhoto(document.GetComponent<DocumentManager>().Properties.Id, frame, OnUpdatePhotoRequest);
#else
			document.GetComponent<DocumentManager>().SetPhoto(Resources.Load<Texture2D>("Images/MultiDocTestImage"));
			DocumentCollection.Instance.Toggle();
			DocumentCollection.Instance.SetFocusedDocument(document);
			updatingDocument = false;
#endif
		}
		else {
#if USE_SERVER
			RequestLauncher.Instance.MatchOrCreateDocument(frame, OnMatchOrCreateRequest);
#else
			DocumentCollection.Instance.AddDocument(croppedPhoto, "-1");
#endif
		}
	}

    private void OnMatchOrCreateRequest(RequestLauncher.RequestAnswerDocument item, bool success) {
        if (success) {
            CameraFrame frame = item.CameraFrameFromBase64();
            Texture2D croppedPhoto = new Texture2D(frame.Resolution.width, frame.Resolution.height);
            croppedPhoto.SetPixels32(frame.Data);
            croppedPhoto.Apply();
            DocumentCollection.Instance.AddDocument(croppedPhoto, item.Id);
        }
        else {
            Debug.Log(item.Error);
			this.photoMode = true;
        }
    }

	private void OnUpdatePhotoRequest(RequestLauncher.RequestAnswerDocument item, bool success) {
		if (success) {
			CameraFrame frame = item.CameraFrameFromBase64();
			Texture2D croppedPhoto = new Texture2D(frame.Resolution.width, frame.Resolution.height);
			croppedPhoto.SetPixels32(frame.Data);
			croppedPhoto.Apply();
			document.GetComponent<DocumentManager>().SetPhoto(croppedPhoto);
			DocumentCollection.Instance.Toggle();
			DocumentCollection.Instance.SetFocusedDocument(document);
		}
		else {
			Debug.Log(item.Error);
			this.photoMode = true;
		}
		updatingDocument = false;
	}

	public void UpdateDocumentPhoto(GameObject document) {
		this.photoMode = true;
		this.updatingDocument = true;
		this.document = document;
	}
}
