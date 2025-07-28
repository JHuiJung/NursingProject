from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse
from google.cloud import speech
from dotenv import load_dotenv
from nursing_llm import get_ai_response
import os

load_dotenv()
app = FastAPI()

os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")

@app.post("/stt")
async def stt_single_question(
    question: str = Form(...),
    audio: UploadFile = File(...)
):
    # 1. STT: 음성 → 텍스트
    client = speech.SpeechClient()
    audio_content = await audio.read()

    audio_config = speech.RecognitionConfig(
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        sample_rate_hertz=16000,
        language_code="ko-KR"
    )
    audio_data = speech.RecognitionAudio(content=audio_content)
    response = client.recognize(config=audio_config, audio=audio_data)

    transcript = ""
    for result in response.results:
        transcript += result.alternatives[0].transcript

    # 2. LangChain 프롬프트 구성
    full_input = (
        "다음은 간호 시뮬레이션 질문에 대한 사용자의 음성 응답입니다.\n\n"
        f"질문: {question}\n\n"
        f"사용자 음성 응답 텍스트: {transcript}\n\n"
        "이 응답이 질문에 적절한지 평가하고, 피드백을 제공하세요. 정답일 경우 ✅로 시작하고, 오답일 경우 ❌로 시작해주세요."
    )

    # 3. LangChain AI 응답
    ai_response = get_ai_response(full_input)

    # 4. 응답 반환
    return JSONResponse(content={
        "transcript": transcript,
        "feedback": ai_response["answer"],
        "is_correct": "✅" in ai_response["answer"],
        "question": question
    })