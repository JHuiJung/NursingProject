from fastapi import FastAPI
from pydantic import BaseModel
from nursing_llm import get_ai_response  # 같은 폴더의 nursing_llm.py에서 get_ai_response 함수 사용

app = FastAPI()

# 요청 모델 정의
class ChatRequest(BaseModel):
    session_id: str
    question: str

# 응답 모델 정의
class ChatResponse(BaseModel):
    answer: str
    correct_count: int
    incorrect_count: int
    total_questions: int
    score_percentage: float

@app.post("/chat", response_model=ChatResponse)
def chat_endpoint(request: ChatRequest):
    # nursing-llm.py의 get_ai_response를 사용
    ai_response = get_ai_response(request.question)
    
    return {
        "answer": ai_response["answer"],
        "correct_count": ai_response["correct_count"],
        "incorrect_count": ai_response["incorrect_count"],
        "total_questions": ai_response["total_questions"],
        "score_percentage": round((ai_response["correct_count"] / ai_response["total_questions"] * 100) if ai_response["total_questions"] > 0 else 0, 1)
    }
