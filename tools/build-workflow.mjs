import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const faq = JSON.parse(fs.readFileSync(path.join(root, "data", "faq-knowledge.json"), "utf8"));
const outFile = path.join(root, "workflows", "ftap-faq-chatbot.n8n.json");

function loadWorkflowSdk() {
  const candidates = [
    path.join(root, "package.json"),
    path.join(root, "..", "n8n-ai-approval-routing-poc", "n8n-runtime", "package.json")
  ];

  for (const candidate of candidates) {
    try {
      return createRequire(candidate)("@n8n/workflow-sdk");
    } catch {
      continue;
    }
  }

  throw new Error("Could not load @n8n/workflow-sdk. Run from this workspace or install the SDK locally.");
}

const { workflow, node, trigger, ifElse, sticky, expr } = loadWorkflowSdk();

const findAnswerCode = String.raw`const body = $json.body ?? $json;
const env = typeof $env === "undefined" ? {} : $env;
const faq = __FAQ_JSON__;
const source = __SOURCE_JSON__;

function firstDefined(...values) {
  return values.find((value) => value !== undefined && value !== null && String(value).trim() !== "");
}

function normalize(value) {
  return String(value ?? "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

const stopWords = new Set(["a", "an", "and", "are", "as", "do", "for", "how", "if", "is", "it", "of", "on", "or", "that", "the", "this", "to", "we", "what", "when", "where", "who", "why", "with"]);
const inScopeTokens = new Set(["access", "acsys", "app", "approval", "approved", "area", "asset", "busy", "certificate", "check", "checkin", "clearance", "cloud", "coe", "code", "company", "connect", "credentials", "dismantling", "email", "emergency", "employment", "escalations", "extraction", "find", "ftap", "geography", "globe", "hotline", "iams", "inquiries", "installation", "landline", "lessor", "mobile", "mop", "nbi", "onedrive", "organization", "otp", "password", "pin", "portal", "pullout", "raawa", "registration", "relay", "reopen", "requests", "server", "site", "sow", "ticket", "upload", "vendor", "wah", "whatsapp", "work"]);
const inScopePhrases = ["after office", "asset point", "check in", "check-in", "close ticket", "could not able to find", "no access right", "not able to connect", "previous site", "server is busy", "specified asset point", "ticket list", "work order"];

function tokenize(value) {
  return normalize(value)
    .split(" ")
    .filter((token) => token.length > 1 && !stopWords.has(token));
}

function scoreEntry(question, entry) {
  const queryTokens = tokenize(question);
  const haystack = new Set(tokenize(entry.question + " " + entry.answer));
  const titleTokens = new Set(tokenize(entry.question));
  let score = 0;

  for (const token of queryTokens) {
    if (haystack.has(token)) score += titleTokens.has(token) ? 3 : 1;
  }

  const haystackText = normalize(entry.question + " " + entry.answer);
  const normalizedQuestion = normalize(question);
  if (normalizedQuestion === normalize(entry.question)) score += 100;
  if (normalizedQuestion.includes("pin") && normalize(entry.question).includes("pin")) score += 6;
  if (normalizedQuestion.includes("emergency") && haystackText.includes("emergency")) score += 4;
  if (normalizedQuestion.includes("whatsapp") && haystackText.includes("whatsapp")) score += 4;
  if (normalizedQuestion.includes("no access right") && entry.number === 5) score += 30;
  if (normalizedQuestion.includes("specified asset point") && entry.number === 5) score += 30;
  if (normalizedQuestion.includes("onedrive") && haystackText.includes("onedrive")) score += 5;
  if (normalizedQuestion.includes("relay") && haystackText.includes("relay")) score += 5;
  if (normalizedQuestion.includes("not able to connect") && entry.number === 14) score += 30;
  if (normalizedQuestion.includes("server is busy") && entry.number === 14) score += 30;
  if (normalizedQuestion.includes("lessor") && haystackText.includes("lessor")) score += 5;
  if (normalizedQuestion.includes("reopen") && haystackText.includes("reopen")) score += 5;
  if (normalizedQuestion.includes("pullout") && haystackText.includes("pullout")) score += 5;
  if (normalizedQuestion.includes("installation") && haystackText.includes("installation")) score += 5;
  if (normalizedQuestion.includes("dismantling") && haystackText.includes("dismantling")) score += 5;

  return { entry, score, queryTokenCount: Math.max(queryTokens.length, 1) };
}

function isInScopeQuestion(question) {
  const normalizedQuestion = normalize(question);
  if (!normalizedQuestion) return false;
  if (faq.some((entry) => normalize(entry.question) === normalizedQuestion)) return true;
  if (faq.some((entry) => normalizedQuestion === String(entry.number) || normalizedQuestion === "faq " + entry.number)) return true;
  if (inScopePhrases.some((phrase) => normalizedQuestion.includes(phrase))) return true;
  return tokenize(normalizedQuestion).some((token) => inScopeTokens.has(token));
}

const question = String(firstDefined(body.message, body.question, body.text, body.prompt, "")).trim();
const sessionId = String(firstDefined(body.sessionId, body.session_id, body.conversationId, "maui-demo"));
const topMatches = faq
  .map((entry) => scoreEntry(question, entry))
  .sort((a, b) => b.score - a.score)
  .slice(0, 3);

const best = topMatches[0];
const confidence = question && best ? Math.min(0.98, Number((best.score / Math.max(best.queryTokenCount * 3, 1)).toFixed(2))) : 0;
const hasAnswer = Boolean(question && best && best.score > 0 && isInScopeQuestion(question));
const fallbackAnswer = "I can only answer questions covered by the FTAP SAM FAQ knowledge base. Please ask about iAMS registration, PIN, emergency access, tickets, OneDrive uploads, relay/server errors, lessor/site access, or other FTAP site access FAQ topics.";
const localAnswer = hasAnswer ? best.entry.answer : fallbackAnswer;
const suggestions = faq.slice(0, 5).map((entry) => entry.question);
const useOpenAI = Boolean(env.OPENAI_API_KEY && !String(env.OPENAI_API_KEY).startsWith("sk-replace"));

const context = topMatches
  .filter((match) => hasAnswer && match.score > 0)
  .map((match) => ({
    number: match.entry.number,
    question: match.entry.question,
    answer: match.entry.answer,
    score: match.score
  }));

const response = {
  sessionId,
  answer: localAnswer,
  confidence,
  source: "local-faq",
  matchedFaq: hasAnswer ? {
    number: best.entry.number,
    question: best.entry.question,
    score: best.score
  } : null,
  citations: context.map((match) => ({
    number: match.number,
    question: match.question
  })),
  suggestedQuestions: hasAnswer ? [] : suggestions,
  generatedAt: new Date().toISOString()
};

const openAiRequest = {
  model: env.OPENAI_MODEL || "gpt-4o-mini",
  instructions: [
    "You are an FTAP Site Access FAQ assistant for GLOBE users.",
    "Use only the supplied FAQ context to identify the best matching FAQ item.",
    "Return a concise confirmation of the FAQ number and do not add procedures, contacts, credentials, or ticket rules.",
    "The workflow will return the exact PDF answer text from the matched FAQ item; do not rewrite the answer."
  ].join("\n"),
  input: [
    {
      role: "user",
      content: [
        {
          type: "input_text",
          text: JSON.stringify({
            question,
            source,
            faq_context: context,
            local_fallback_answer: localAnswer
          })
        }
      ]
    }
  ],
  temperature: 0.1,
  max_output_tokens: 500
};

return [{
  json: {
    question,
    sessionId,
    response,
    useOpenAI: useOpenAI && hasAnswer,
    openAiRequest,
    faqContext: context
  }
}];`
  .replace("__FAQ_JSON__", JSON.stringify(faq.entries))
  .replace("__SOURCE_JSON__", JSON.stringify({
    title: faq.title,
    sourceDocument: faq.sourceDocument,
    audience: faq.audience,
    effectiveDate: faq.effectiveDate
  }));

const formatOpenAiCode = String.raw`const base = $("Find FAQ Answer").first().json;
const response = $input.first().json;

function getText(resp) {
  if (typeof resp?.output_text === "string") return resp.output_text;
  if (Array.isArray(resp?.output)) {
    for (const output of resp.output) {
      if (Array.isArray(output?.content)) {
        for (const content of output.content) {
          if (typeof content?.text === "string") return content.text;
        }
      }
    }
  }
  if (typeof resp?.choices?.[0]?.message?.content === "string") {
    return resp.choices[0].message.content;
  }
  return "";
}

const modelAnswer = response?.error ? "" : getText(response).trim();
const errorMessage = response?.error?.message || "";

return [{
  json: {
    ...base,
    openAiRaw: response,
    response: {
      ...base.response,
      answer: base.response.answer,
      source: modelAnswer ? "openai-grounded-faq-exact" : "local-faq",
      openAi: {
        used: Boolean(modelAnswer),
        mode: "route-only-exact-pdf-answer",
        error: errorMessage
      }
    }
  }
}];`;

const webhook = trigger({
  type: "n8n-nodes-base.webhook",
  version: 2.1,
  config: {
    name: "Webhook - MAUI FAQ Chat",
    parameters: {
      httpMethod: "POST",
      path: "ftap-faq-chat",
      responseMode: "responseNode",
      options: {}
    }
  },
  output: [{ body: { message: "What is the iAMS PIN code?", sessionId: "demo-1" } }]
});

const findAnswer = node({
  type: "n8n-nodes-base.code",
  version: 2,
  config: {
    name: "Find FAQ Answer",
    parameters: {
      mode: "runOnceForAllItems",
      jsCode: findAnswerCode
    }
  },
  output: [{
    question: "What is the iAMS PIN code?",
    sessionId: "demo-1",
    useOpenAI: false,
    response: {
      answer: "1234",
      confidence: 0.98,
      source: "local-faq"
    }
  }]
});

const useOpenAi = ifElse({
  version: 2.2,
  config: {
    name: "IF - Use OpenAI?",
    parameters: {
      conditions: {
        options: { caseSensitive: true, leftValue: "", typeValidation: "strict" },
        conditions: [
          {
            leftValue: expr("{{ $json.useOpenAI }}"),
            operator: { type: "boolean", operation: "true" }
          }
        ],
        combinator: "and"
      }
    }
  }
});

const openAi = node({
  type: "n8n-nodes-base.httpRequest",
  version: 4.4,
  config: {
    name: "OpenAI - Route FAQ Answer",
    parameters: {
      method: "POST",
      url: "https://api.openai.com/v1/responses",
      sendHeaders: true,
      headerParameters: {
        parameters: [
          { name: "Authorization", value: expr("{{ 'Bearer ' + $env.OPENAI_API_KEY }}") },
          { name: "Content-Type", value: "application/json" }
        ]
      },
      sendBody: true,
      specifyBody: "json",
      jsonBody: expr("{{ $json.openAiRequest }}"),
      options: { timeout: 30000 }
    },
    retryOnFail: true,
    maxTries: 2,
    waitBetweenTries: 1500,
    continueOnFail: true
  },
  output: [{ output_text: "Matched FAQ #2." }]
});

const formatOpenAi = node({
  type: "n8n-nodes-base.code",
  version: 2,
  config: {
    name: "Format OpenAI Answer",
    parameters: {
      mode: "runOnceForAllItems",
      jsCode: formatOpenAiCode
    }
  },
  output: [{ response: { answer: "1234", source: "openai-grounded-faq-exact" } }]
});

const respond = node({
  type: "n8n-nodes-base.respondToWebhook",
  version: 1.5,
  config: {
    name: "Respond to MAUI",
    parameters: {
      respondWith: "json",
      responseBody: expr("{{ $json.response }}"),
      options: {
        responseCode: 200,
        responseHeaders: {
          entries: [
            { name: "Access-Control-Allow-Origin", value: "*" }
          ]
        }
      }
    }
  },
  output: [{ answer: "1234", source: "local-faq" }]
});

const note = sticky(
  "## FTAP AI Chatbot PoC\nPOST from .NET MAUI to `/webhook-test/ftap-faq-chat` while testing or `/webhook/ftap-faq-chat` after activation.\n\nPayload: `{ \"message\": \"What is the iAMS PIN code?\", \"sessionId\": \"demo-1\" }`.\n\nFinal answers are returned exactly from the extracted PDF FAQ. OpenAI can be used for route/grounding metadata when `OPENAI_API_KEY` is set.",
  [webhook, findAnswer, respond],
  { color: 4 }
);

const faqWorkflow = workflow("ftap-ai-chatbot", "FTAP AI Chatbot", {
  executionOrder: "v1"
})
  .add(note)
  .add(webhook)
  .to(findAnswer)
  .to(useOpenAi
    .onTrue(openAi.to(formatOpenAi.to(respond)))
    .onFalse(respond));

const validation = faqWorkflow.validate();
if (!validation.valid) {
  console.error(JSON.stringify(validation, null, 2));
  process.exit(1);
}

fs.mkdirSync(path.dirname(outFile), { recursive: true });
fs.writeFileSync(outFile, JSON.stringify(faqWorkflow.toJSON({ tidyUp: true }), null, 2) + "\n");
console.log(`Wrote ${outFile}`);
