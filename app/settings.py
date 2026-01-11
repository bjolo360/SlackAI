from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="SLACKAI_", case_sensitive=False)

    database_url: str = "postgresql+asyncpg://postgres:postgres@localhost:5432/slackai"
    slack_token: str = ""
    sync_interval_seconds: int = 300
    slack_api_base: str = "https://slack.com/api"
    request_timeout_seconds: int = 30
    max_retries: int = 5


settings = Settings()
