from dotenv import load_dotenv
import os

from langchain_community.document_loaders import Docx2txtLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_openai import ChatOpenAI, OpenAIEmbeddings
from langchain_upstage import UpstageEmbeddings
from pinecone import Pinecone
from langchain_pinecone import PineconeVectorStore
from langchain.chains import ConversationalRetrievalChain
from langchain.prompts import PromptTemplate
from langchain_community.chat_message_histories import ChatMessageHistory
from langchain_core.chat_history import BaseChatMessageHistory
from langchain_core.runnables.history import RunnableWithMessageHistory

# 1. 환경 변수 및 OpenAI API 키 로드
load_dotenv()
openai_api_key = os.environ.get("OPENAI_API_KEY")

# 2. 문서 로딩 및 분할 (예시: DOCX)
def load_doc_from_path(path: str):
    docs = Docx2txtLoader(path).load_and_split(
        text_splitter=RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=200)
    )
    return docs

# 3. 벡터스토어 (Pinecone)
embedding = UpstageEmbeddings(model="solar-embedding-1-large")
pinecone_api_key = os.environ.get("PINECONE_API_KEY")
pinecone_index = "nursing-upsate-index"
# host = os.getenv("PINECONE_HOST")
pc = Pinecone(api_key=pinecone_api_key)
vectorstore = PineconeVectorStore.from_existing_index(index_name=pinecone_index, embedding=embedding)

# 4. LLM 및 프롬프트
llm = ChatOpenAI(model="gpt-4.1-mini", openai_api_key=openai_api_key)

rag_prompt = PromptTemplate(
    input_variables=["context", "question"],
    template="""
    
당신은 간호학 실습을 도와주는 시뮬레이션 AI입니다.
당신의 역할은 다음과 같습니다:

1. 각 항목에서 사용자의 답이 정답인지 확인합니다.
2. 정답일 경우 → ✅ "정확하게 확인하였습니다. 다음 단계로 이동하세요."
3. 오답일 경우 → ❌ 항목별 피드백을 아래 기준으로 제공합니다.

---

피드백 기준:
정답과 오답을 비교해서 오답에 대한 피드백을 제공합니다.

---
참고 문서:
{context}

질문:
{question}

답변:
"""
)

condense_prompt = PromptTemplate(
    input_variables=["chat_history", "question"],
    template="""
이전 대화 내역:
{chat_history}

사용자가 다음과 같은 질문을 했습니다:
{question}

위 대화를 참고하여, 사용자의 의도를 최대한 명확하게 반영한 질문으로 바꿔주세요.
"""
)

# 5. ConversationalRetrievalChain 구성
base_chain = ConversationalRetrievalChain.from_llm(
    llm=llm,
    retriever=vectorstore.as_retriever(search_kwargs={"k": 3}),
    condense_question_prompt=condense_prompt,
    combine_docs_chain_kwargs={"prompt": rag_prompt},
    return_source_documents=False
)

# 6. 세션 기반 히스토리 관리
store = {}

def get_session_history(session_id: str) -> BaseChatMessageHistory:
    if session_id not in store:
        store[session_id] = ChatMessageHistory()
    return store[session_id]

# 7. RunnableWithMessageHistory로 감싸기
conversational_chain = RunnableWithMessageHistory(
    base_chain,
    get_session_history,
    input_messages_key="question",
    history_messages_key="chat_history",
    output_messages_key="answer"
).pick("answer")

if __name__ == "__main__":
    # 예시 세션 ID와 질문
    session_id = "test-session"
    question = '''
Q1. 약물 이름은 무엇인가요?  
사용자 선택: Cefotaxime 0.5g Vial  

Q2. 투약 경로는 무엇인가요?  
사용자 선택: 근육주사 (IM)  

Q3. 1회 용량은 얼마인가요?  
사용자 선택: 1kg 당 500mg 하루 8시간 간격  

Q4. 정규 투약 스케줄 시간과 현재시간이 일치하나요?  
사용자 선택: 화면 시계가 5:40am으로 세팅되어 있음  

Q5. 이 약물의 성분은 무엇인가요?  
사용자 선택: 젠타마이신계 항생물질 (Gentamicin)  

Q6. 이 약물은 어떤 경로로 투여 되나요?  
사용자 선택: 정맥주사 또는 근육주사  

Q7. 이 약물은 투여 시 희석이 필요한가요?  
사용자 선택: 예 반드시 희석 후 투여해야 합니다.  

Q8. 유효기간은 남아 있나요?  
사용자 선택: 부적합. 약물의 유효기간이 만료되었습니다.  

Q9. 약물 용기의 상태는 어떠한가요?  
사용자 선택: 적합, 용기가 개봉되지 않고 밀봉이 되어있으며 이상 여부 없습니다.  
'''

    # 대화 실행
    answer = conversational_chain.invoke(
        {"question": question},
        config={"configurable": {"session_id": session_id}}
    )
    print("AI 답변:", answer)