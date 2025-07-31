from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse
import os, requests
from dotenv import load_dotenv
from nursing_llm import get_ai_response

load_dotenv()
app = FastAPI()

CLOVA_URL = "https://clovaspeech-gw.ncloud.com/recog/v1/stt"
CLOVA_API_KEY = os.getenv("CLOVA_SPEECH_SECRET")  # Secret Key

@app.post("/clova_stt")
async def clova_stt(
    question: str = Form(...),
    audio: UploadFile = File(...)
):
    audio_bytes = await audio.read()
    headers = {
        "X-CLOVASPEECH-API-KEY": CLOVA_API_KEY,
        "Content-Type": "application/octet-stream"
    }
    params = {"lang": "Kor",
              "boostings": "환아\t이하트\t239845\t심장\t주사\t팔\t수액줄"}

    # 1. Clova STT 요청 (short-sentence API)
    resp = requests.post(CLOVA_URL, params=params, headers=headers, data=audio_bytes)
    if resp.status_code != 200:
        return JSONResponse(status_code=500, content={"error": f"Clova STT 실패: {resp.status_code}"})
    result = resp.json()
    transcript = result.get("text", "")

    # 2. LangChain 평가 구성
    full_input = (
        f"질문: {question}\n\n"
        f"응답: {transcript}\n\n"
        "이 응답이 적절한지 평가하고, 피드백을 주세요."
    )

    ai_response = get_ai_response(full_input)
    feedback = ai_response.get("answer", "")

    return JSONResponse({
        "transcript": transcript,
        "feedback": feedback,
        "is_correct": feedback,
        "question": question
    })