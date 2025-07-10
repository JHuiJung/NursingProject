from rag_pipeline import store, llm
from langchain.prompts import PromptTemplate
from langchain_core.chat_history import BaseMessage

def generate_letter(session_id: str) -> str:
    """
    주어진 세션 ID를 기반으로 대화 기록을 불러와,
    그 내용을 바탕으로 시민군 시점의 편지를 작성하는 함수.
    """
    history = store.get(session_id)
    if history is None:
        return "해당 세션에 대한 대화 기록이 없습니다."

    messages = history.messages  # List[BaseMessage]

    formatted_history = ""
    for msg in messages:
        role = "사용자" if msg.type == "human" else "시민군"
        formatted_history += f"{role}: {msg.content}\n"

    letter_prompt = PromptTemplate(
        input_variables=["chat_history"],
        template="""
    다음은 지금까지의 대화 기록입니다:

    {chat_history}

    이 대화를 바탕으로 사용자에게 보내는 편지를 작성해주세요.
    당신은 1980년 5월 광주의 시민군이며, 대화를 나눈 사용자에게 당신의 감정과 진심을 담아 편지를 씁니다.
    다음 조건을 반드시 지켜주세요:

    1. 편지는 질문자의 언어로 작성합니다. (예: 사용자가 영어로 질문하면 영어로 편지를 작성하세요.)
    2. 편지는 **완결된 구조**를 갖춰야 하며, **8문장을 넘기지 마세요.**
    3. 다음과 같은 정서를 담아야 합니다:
       - 당신이 겪은 슬픔과 고통
       - 함께한 대화에서 느낀 **연대감과 위로**
       - 과거를 기억하는 중요성
       - 사용자가 지금 **현장을 여행 중일 수 있다는 전제 하에**, 장소와의 감정적 연결
       - 마지막에는 **희망적인 메시지로 마무리**해 주세요.

    편지 형식은 다음을 따릅니다:

    ---

    친애하는 친구에게,

    (여기에 진심 어린 편지 본문)

    시민군 일동 드림.
    """
    )


    prompt = letter_prompt.format(chat_history=formatted_history)
    letter = llm.invoke(prompt)
    return letter.content