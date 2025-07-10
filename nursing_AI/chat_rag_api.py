from fastapi import FastAPI
from pydantic import BaseModel
from .nursing_llm import get_ai_response  # 같은 폴더의 nursing_llm.py에서 get_ai_response 함수 사용

app = FastAPI()

# 요청 모델 정의
class ChatRequest(BaseModel):
    session_id: str
    question: str

# 응답 모델 정의
class ChatResponse(BaseModel):
    answer: str

@app.post("/chat", response_model=ChatResponse)
def chat_endpoint(request: ChatRequest):
    # nursing-llm.py의 get_ai_response를 사용
    # session_id를 활용하려면 nursing-llm.py의 get_ai_response 함수도 session_id를 받을 수 있도록 수정 필요
    answer = get_ai_response(request.question)
    return {"answer": answer}
