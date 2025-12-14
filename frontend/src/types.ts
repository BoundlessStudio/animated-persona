export type Conversation = {
  id: string;
  url: string;
  startedAt: string;
  expiresAt: string;
};

export type MachineInfo = {
  machineId: string;
  appName: string;
  region: string;
  serviceUrl: string;
  port: number;
};

export type ChatMessage = {
  id: string;
  role: 'user' | 'assistant' | 'system' | 'tool';
  content: string;
  createdAt: string;
};

export type AgentResponse = {
  reply: string;
  memory?: ChatMessage[];
  sessionId?: string;
  toolLog?: string;
};
